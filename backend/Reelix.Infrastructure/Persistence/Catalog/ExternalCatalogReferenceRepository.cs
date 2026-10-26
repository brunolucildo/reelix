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
