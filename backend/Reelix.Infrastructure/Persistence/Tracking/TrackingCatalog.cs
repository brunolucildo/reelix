using Reelix.Application.Catalog;
using Reelix.Application.Tracking;

namespace Reelix.Infrastructure.Persistence.Tracking;

public sealed class TrackingCatalog(
    CatalogService catalog,
    ITrackingRepository tracking) : ITrackingCatalog
{
    public async Task<CatalogSeason?> GetSeasonAsync(
        Guid seriesId,
        int seasonNumber,
        CatalogProvider provider,
        CancellationToken cancellationToken)
    {
        var externalId = await tracking.GetSeriesExternalIdAsync(
            seriesId,
            provider,
            cancellationToken);
        return externalId is null
            ? null
            : await catalog.GetSeasonAsync(
                provider,
                externalId,
                seasonNumber,
                "pt-BR",
                cancellationToken);
    }
}
