using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Catalog;
using Reelix.Application.Tracking;
using Reelix.Domain.Catalog;
using Reelix.Domain.Tracking;

namespace Reelix.Infrastructure.Persistence.Tracking;

public sealed class TrackingRepository(ReelixDbContext dbContext) : ITrackingRepository
{
    public Task<bool> CatalogReferenceExistsAsync(
        Guid id,
        CatalogReferenceType type,
        CancellationToken cancellationToken) =>
        dbContext.Set<ExternalCatalogReference>()
            .AnyAsync(
                reference =>
                    reference.Id == id &&
                    reference.ContentType == type.ToString().ToLowerInvariant(),
                cancellationToken);

    public Task<string?> GetSeriesExternalIdAsync(
        Guid seriesId,
        CatalogProvider provider,
        CancellationToken cancellationToken) =>
        dbContext.Set<ExternalCatalogReference>()
            .Where(reference =>
                reference.Id == seriesId &&
                reference.ContentType == "series" &&
                reference.Provider == provider.ToString().ToLowerInvariant())
            .Select(reference => reference.ExternalId)
            .SingleOrDefaultAsync(cancellationToken);

    public Task<TrackingRecord?> GetAsync(
        Guid accountId,
        Guid targetId,
        CancellationToken cancellationToken) =>
        dbContext.Set<TrackingRecord>()
            .SingleOrDefaultAsync(
                record => record.AccountId == accountId && record.TargetId == targetId,
                cancellationToken);

    public async Task<TrackingRecord> AddOrUpdateAsync(
        TrackingRecord record,
        CancellationToken cancellationToken)
    {
        var existing = await GetAsync(record.AccountId, record.TargetId, cancellationToken);
        if (existing is not null)
        {
            existing.CorrectWatchedAt(record.WatchedAt);
            await dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        dbContext.Set<TrackingRecord>().Add(record);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return record;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "UX_tracking_records_account_target",
            })
        {
            dbContext.Entry(record).State = EntityState.Detached;
            var concurrent = await GetAsync(record.AccountId, record.TargetId, cancellationToken);
            if (concurrent is null)
            {
                throw;
            }

            if (concurrent.ItemType != record.ItemType || concurrent.SeriesId != record.SeriesId)
            {
                throw new TrackingSeriesMismatchException();
            }

            concurrent.CorrectWatchedAt(record.WatchedAt);
            await dbContext.SaveChangesAsync(cancellationToken);
            return concurrent;
        }
    }

    public async Task RemoveAsync(
        TrackingRecord record,
        CancellationToken cancellationToken)
    {
        dbContext.Set<TrackingRecord>().Remove(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TrackingPage<TrackingHistoryEntry>> GetHistoryAsync(
        Guid accountId,
        TrackingItemType? type,
        int page,
        int pageSize,
        TrackingHistoryOrder order,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<TrackingRecord>()
            .AsNoTracking()
            .Where(record => record.AccountId == accountId);
        if (type is not null)
        {
            query = query.Where(record => record.ItemType == type);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var ordered = order == TrackingHistoryOrder.OldestFirst
            ? query.OrderBy(record => record.WatchedAt).ThenBy(record => record.Id)
            : query.OrderByDescending(record => record.WatchedAt).ThenByDescending(record => record.Id);
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(record => new TrackingHistoryEntry(
                record.Id,
                record.ItemType,
                record.TargetId,
                record.SeriesId,
                record.WatchedAt))
            .ToListAsync(cancellationToken);

        var referenceIds = items
            .SelectMany(item => item.SeriesId is { } seriesId
                ? new[] { item.TargetId, seriesId }
                : new[] { item.TargetId })
            .Distinct()
            .ToArray();
        var references = await dbContext.ExternalCatalogReferences.AsNoTracking()
            .Where(reference => referenceIds.Contains(reference.Id))
            .ToDictionaryAsync(reference => reference.Id, cancellationToken);
        var enrichedItems = items.Select(item =>
        {
            references.TryGetValue(item.TargetId, out var targetReference);
            ExternalCatalogReference? seriesReference = null;
            if (item.SeriesId is { } seriesId)
            {
                references.TryGetValue(seriesId, out seriesReference);
            }

            var titleReference = item.Type == TrackingItemType.Movie
                ? targetReference
                : seriesReference;
            return item with
            {
                Provider = titleReference?.Provider,
                ExternalId = item.Type == TrackingItemType.Movie ? targetReference?.ExternalId : null,
                SeriesExternalId = seriesReference?.ExternalId,
            };
        }).ToArray();

        return new TrackingPage<TrackingHistoryEntry>(enrichedItems, page, pageSize, totalCount);
    }

    public async Task<TrackingPage<TrackedSeries>> GetSeriesAsync(
        Guid accountId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var episodes = dbContext.Set<TrackingRecord>()
            .AsNoTracking()
            .Where(record =>
                record.AccountId == accountId &&
                record.ItemType == TrackingItemType.Episode &&
                record.SeriesId != null);
        var totalCount = await episodes
            .Select(record => record.SeriesId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
        var items = await episodes
            .GroupBy(record => record.SeriesId!.Value)
            .Select(group => new
            {
                SeriesId = group.Key,
                WatchedEpisodeCount = group.Count(),
                LastWatchedAt = group.Max(record => record.WatchedAt),
                LatestEpisodeId = group.OrderByDescending(record => record.WatchedAt)
                    .ThenByDescending(record => record.Id).Select(record => record.TargetId).First(),
            })
            .OrderByDescending(group => group.LastWatchedAt)
            .ThenBy(group => group.SeriesId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(group => new TrackedSeries(group.SeriesId, group.WatchedEpisodeCount,
                null, null, group.LatestEpisodeId, null))
            .ToListAsync(cancellationToken);

        var seriesIds = items.Select(item => item.SeriesId).ToArray();
        var references = await dbContext.ExternalCatalogReferences.AsNoTracking()
            .Where(reference => seriesIds.Contains(reference.Id))
            .ToDictionaryAsync(reference => reference.Id, cancellationToken);
        var enriched = new List<TrackedSeries>(items.Count);
        foreach (var item in items)
        {
            if (!references.TryGetValue(item.SeriesId, out var reference))
            {
                enriched.Add(item);
                continue;
            }

            var prefix = reference.ExternalId + ":";
            var seasonIds = await dbContext.ExternalCatalogReferences.AsNoTracking()
                .Where(season => season.Provider == reference.Provider &&
                    season.ContentType == "season" && season.ExternalId.StartsWith(prefix))
                .Select(season => season.ExternalId)
                .ToArrayAsync(cancellationToken);
            var seasonNumbers = seasonIds.Select(id => int.TryParse(id[prefix.Length..], out var number)
                    ? (int?)number : null)
                .Where(number => number >= 0).Select(number => number!.Value).Distinct().Order().ToArray();
            enriched.Add(item with
            {
                Provider = reference.Provider,
                ExternalId = reference.ExternalId,
                KnownSeasonNumbers = seasonNumbers,
            });
        }

        return new TrackingPage<TrackedSeries>(enriched, page, pageSize, totalCount);
    }

    public Task<int> GetWatchedEpisodeCountAsync(
        Guid accountId,
        Guid seriesId,
        CancellationToken cancellationToken) =>
        dbContext.Set<TrackingRecord>()
            .CountAsync(
                record =>
                    record.AccountId == accountId &&
                    record.ItemType == TrackingItemType.Episode &&
                    record.SeriesId == seriesId,
                cancellationToken);

    public async Task<HashSet<Guid>> GetWatchedEpisodeIdsAsync(
        Guid accountId,
        Guid seriesId,
        IReadOnlyCollection<Guid> episodeIds,
        CancellationToken cancellationToken)
    {
        if (episodeIds.Count == 0)
        {
            return [];
        }

        var ids = await dbContext.Set<TrackingRecord>()
            .Where(record =>
                record.AccountId == accountId &&
                record.ItemType == TrackingItemType.Episode &&
                record.SeriesId == seriesId &&
                episodeIds.Contains(record.TargetId))
            .Select(record => record.TargetId)
            .ToListAsync(cancellationToken);
        return ids.ToHashSet();
    }
}
