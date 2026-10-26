using Reelix.Application.Catalog;

namespace Reelix.UnitTests.Catalog;

public sealed class CatalogServiceTests
{
    [Fact]
    public async Task GetSeason_AssignsStableInternalIdsToSeriesSeasonAndEpisodes()
    {
        var provider = new StubCatalogProvider();
        var references = new StubReferenceRepository();
        var service = new CatalogService([provider], references);

        var season = await service.GetSeasonAsync(
            CatalogProvider.Tmdb,
            "1396",
            1,
            "pt-BR",
            CancellationToken.None);

        Assert.NotNull(season);
        Assert.Equal(references.IdFor("1396", CatalogReferenceType.Series), season.SeriesId);
        Assert.Equal(
            references.IdFor("1396:1", CatalogReferenceType.Season),
            season.Id);
        var episode = Assert.Single(season.Episodes);
        Assert.Equal(
            references.IdFor("tt-episode-1", CatalogReferenceType.Episode),
            episode.Id);
        Assert.Contains(
            new CatalogReferenceKey("1396:1", CatalogReferenceType.Season),
            references.LastRequestedReferences);
    }

    private sealed class StubCatalogProvider : ICatalogProvider
    {
        public CatalogProvider Provider => CatalogProvider.Tmdb;

        public Task<ProviderCatalogPage> SearchAsync(
            string query,
            CatalogContentType? contentType,
            int page,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ProviderCatalogPage([], page, 0, 0));

        public Task<ProviderCatalogTitle?> GetDetailsAsync(
            CatalogContentType contentType,
            string externalId,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProviderCatalogTitle?>(null);

        public Task<ProviderCatalogSeason?> GetSeasonAsync(
            string externalSeriesId,
            int seasonNumber,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProviderCatalogSeason?>(
                new ProviderCatalogSeason(
                    "Season 1",
                    1,
                    new DateOnly(2008, 1, 20),
                    [
                        new ProviderCatalogEpisode(
                            "tt-episode-1",
                            1,
                            "Pilot",
                            null,
                            new DateOnly(2008, 1, 20),
                            null),
                    ]));
    }

    private sealed class StubReferenceRepository : IExternalCatalogReferenceRepository
    {
        private readonly Dictionary<CatalogReferenceKey, Guid> _ids = [];

        public IReadOnlyCollection<CatalogReferenceKey> LastRequestedReferences { get; private set; } = [];

        public Guid IdFor(string externalId, CatalogReferenceType type) =>
            _ids[new CatalogReferenceKey(externalId, type)];

        public Task<IReadOnlyDictionary<CatalogReferenceKey, Guid>> GetOrCreateIdsAsync(
            CatalogProvider provider,
            IReadOnlyCollection<CatalogReferenceKey> references,
            CancellationToken cancellationToken)
        {
            LastRequestedReferences = references;
            foreach (var reference in references)
            {
                _ids.TryAdd(reference, Guid.NewGuid());
            }

            return Task.FromResult<IReadOnlyDictionary<CatalogReferenceKey, Guid>>(
                references.ToDictionary(reference => reference, reference => _ids[reference]));
        }
    }
}
