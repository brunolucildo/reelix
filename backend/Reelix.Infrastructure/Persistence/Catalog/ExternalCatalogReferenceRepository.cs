using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Catalog;
using Reelix.Domain.Catalog;

namespace Reelix.Infrastructure.Persistence.Catalog;

public sealed class ExternalCatalogReferenceRepository(ReelixDbContext dbContext)
    : IExternalCatalogReferenceRepository
{
    public async Task<IReadOnlyDictionary<CatalogReferenceKey, Guid>> GetOrCreateIdsAsync(
        CatalogProvider provider,
        IReadOnlyCollection<CatalogReferenceKey> references,
        CancellationToken cancellationToken)
    {
        var uniqueReferences = references.Distinct().ToArray();
        if (uniqueReferences.Length == 0)
        {
            return new Dictionary<CatalogReferenceKey, Guid>();
        }

        var providerName = provider.ToString().ToLowerInvariant();
        var externalIds = uniqueReferences.Select(reference => reference.ExternalId).Distinct().ToArray();
        var existing = await FindAsync(providerName, externalIds, cancellationToken);
        var existingKeys = existing
            .Select(ToKey)
            .ToHashSet();
        var missing = uniqueReferences
            .Where(reference => !existingKeys.Contains(reference))
            .Select(reference => ExternalCatalogReference.Create(
                providerName,
                reference.ExternalId,
                reference.Type.ToString()))
            .ToArray();

        dbContext.ExternalCatalogReferences.AddRange(missing);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            existing.AddRange(missing);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            })
        {
            foreach (var reference in missing)
            {
                dbContext.Entry(reference).State = EntityState.Detached;
            }

            existing = await FindAsync(providerName, externalIds, cancellationToken);
        }

        var ids = existing.ToDictionary(ToKey, reference => reference.Id);
        return uniqueReferences.ToDictionary(reference => reference, reference => ids[reference]);
    }

    public async Task UpdateMetadataAsync(
        CatalogProvider provider,
        IReadOnlyCollection<CatalogMetadataSnapshot> snapshots,
        CancellationToken cancellationToken)
    {
        var metadata = snapshots
            .Where(snapshot =>
                snapshot.RuntimeMinutes is > 0 and <= 1440 ||
                snapshot.GenreIds.Any(id => id > 0))
            .GroupBy(snapshot => snapshot.Reference)
            .Select(group => group.Last())
            .ToArray();
        if (metadata.Length == 0)
        {
            return;
        }

        var providerName = provider.ToString().ToLowerInvariant();
        var references = await FindAsync(
            providerName,
            metadata.Select(item => item.Reference.ExternalId).Distinct().ToArray(),
            cancellationToken);
        var referencesByKey = references.ToDictionary(ToKey);
        var updates = metadata
            .Where(snapshot => referencesByKey.ContainsKey(snapshot.Reference))
            .Select(snapshot => new
            {
                Reference = referencesByKey[snapshot.Reference],
                RuntimeMinutes = snapshot.RuntimeMinutes is > 0 and <= 1440
                    ? snapshot.RuntimeMinutes
                    : null,
                GenreIds = snapshot.GenreIds.Where(id => id > 0).Distinct().ToArray(),
            })
            .ToArray();
        if (updates.Length == 0)
        {
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var referenceIds = updates.Select(update => update.Reference.Id).Distinct().ToArray();
        await dbContext.ExternalCatalogReferences
            .FromSqlInterpolated(
                $"SELECT * FROM public.catalog_external_references WHERE id = ANY ({referenceIds}) ORDER BY id FOR UPDATE")
            .ToArrayAsync(cancellationToken);
        var existingGenres = await dbContext.CatalogReferenceGenres
            .Where(genre => referenceIds.Contains(genre.CatalogReferenceId))
            .ToListAsync(cancellationToken);
        var genresByReference = existingGenres
            .GroupBy(genre => genre.CatalogReferenceId)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(genre => genre.GenreId));
        var genresToRemove = new List<CatalogReferenceGenre>();
        var genresToAdd = new List<CatalogReferenceGenre>();
        foreach (var update in updates)
        {
            genresByReference.TryGetValue(update.Reference.Id, out var currentGenres);
            currentGenres ??= [];
            var hasGenreIds = update.GenreIds.Length > 0;
            var currentGenreIds = currentGenres.Keys.ToHashSet();
            var genreSetChanged = hasGenreIds &&
                !currentGenreIds.SetEquals(update.GenreIds);
            var runtimeChanged = update.RuntimeMinutes is not null &&
                update.Reference.RuntimeMinutes != update.RuntimeMinutes;
            update.Reference.UpdateMetadata(
                update.RuntimeMinutes,
                hasGenreIds,
                runtimeChanged || genreSetChanged);

            if (!genreSetChanged)
            {
                continue;
            }

            var desiredGenreIds = update.GenreIds.ToHashSet();
            genresToRemove.AddRange(
                currentGenres.Values.Where(genre => !desiredGenreIds.Contains(genre.GenreId)));
            genresToAdd.AddRange(
                desiredGenreIds
                    .Where(genreId => !currentGenreIds.Contains(genreId))
                    .Select(genreId => CatalogReferenceGenre.Create(update.Reference.Id, genreId)));
        }

        dbContext.CatalogReferenceGenres.RemoveRange(genresToRemove);
        dbContext.CatalogReferenceGenres.AddRange(genresToAdd);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task LinkEpisodesToSeriesAsync(
        Guid seriesId,
        IReadOnlyCollection<Guid> episodeIds,
        CancellationToken cancellationToken)
    {
        if (seriesId == Guid.Empty || episodeIds.Any(id => id == Guid.Empty))
        {
            throw new ArgumentException("Informe identificadores válidos de série e episódios.");
        }

        var uniqueEpisodeIds = episodeIds.Distinct().ToArray();
        var existing = await dbContext.CatalogEpisodeSeries
            .Where(link => uniqueEpisodeIds.Contains(link.EpisodeId))
            .ToDictionaryAsync(link => link.EpisodeId, cancellationToken);
        if (existing.Values.Any(link => link.SeriesId != seriesId))
        {
            throw new InvalidOperationException(
                "O catálogo retornou um episódio associado a mais de uma série.");
        }

        var missing = uniqueEpisodeIds
            .Where(id => !existing.ContainsKey(id))
            .Select(id => CatalogEpisodeSeries.Create(id, seriesId))
            .ToArray();
        if (missing.Length == 0)
        {
            return;
        }

        dbContext.CatalogEpisodeSeries.AddRange(missing);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<List<ExternalCatalogReference>> FindAsync(
        string provider,
        string[] externalIds,
        CancellationToken cancellationToken) =>
        dbContext.ExternalCatalogReferences
            .Where(reference =>
                reference.Provider == provider &&
                externalIds.Contains(reference.ExternalId))
            .ToListAsync(cancellationToken);

    private static CatalogReferenceKey ToKey(ExternalCatalogReference reference) =>
        new(reference.ExternalId, Enum.Parse<CatalogReferenceType>(reference.ContentType, ignoreCase: true));
}
