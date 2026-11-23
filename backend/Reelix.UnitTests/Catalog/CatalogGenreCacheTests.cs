using Reelix.Application.Catalog;
using Reelix.Infrastructure.Catalog;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Catalog;

/// <summary>
/// The genre cache (spec 0013, R11): six hours by the injected clock, keyed by provider, content
/// type and language, and owned by the instance (no process-wide static state).
/// </summary>
public sealed class CatalogGenreCacheTests
{
    [Fact]
    public async Task GetGenres_CallsTheProviderOnceWithinSixHours()
    {
        var clock = FixedTimeProvider.AtDefault();
        var provider = new CountingGenreProvider();
        var service = CreateService(provider, clock);

        var first = await GetAsync(service);
        clock.Advance(TimeSpan.FromHours(5) + TimeSpan.FromMinutes(59));
        var second = await GetAsync(service);

        Assert.Equal(1, provider.Calls);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task GetGenres_CallsTheProviderAgainWhenTheEntryIsOlderThanSixHours()
    {
        var clock = FixedTimeProvider.AtDefault();
        var provider = new CountingGenreProvider();
        var service = CreateService(provider, clock);

        await GetAsync(service);
        clock.Advance(TimeSpan.FromHours(6) + TimeSpan.FromSeconds(1));
        await GetAsync(service);

        Assert.Equal(2, provider.Calls);
    }

    [Fact]
    public async Task GetGenres_KeepsOneEntryPerContentTypeAndLanguage()
    {
        var provider = new CountingGenreProvider();
        var service = CreateService(provider, FixedTimeProvider.AtDefault());

        await service.GetGenresAsync(CatalogProvider.Tmdb, CatalogContentType.Movie, "pt-BR", default);
        await service.GetGenresAsync(CatalogProvider.Tmdb, CatalogContentType.Series, "pt-BR", default);
        await service.GetGenresAsync(CatalogProvider.Tmdb, CatalogContentType.Movie, "en-US", default);
        await service.GetGenresAsync(CatalogProvider.Tmdb, CatalogContentType.Movie, "pt-BR", default);

        Assert.Equal(3, provider.Calls);
    }

    [Fact]
    public async Task GetGenres_DoesNotShareTheCacheBetweenInstances()
    {
        var clock = FixedTimeProvider.AtDefault();
        var firstHost = new CountingGenreProvider();
        var secondHost = new CountingGenreProvider();

        await GetAsync(CreateService(firstHost, clock));
        await GetAsync(CreateService(secondHost, clock));

        Assert.Equal(1, firstHost.Calls);
        Assert.Equal(1, secondHost.Calls);
    }

    [Fact]
    public async Task GetGenres_DoesNotCacheAFailure()
    {
        var provider = new CountingGenreProvider { Failure = new CatalogProviderException(
            "catalog_unavailable", "Indisponível.", 503) };
        var service = CreateService(provider, FixedTimeProvider.AtDefault());

        await Assert.ThrowsAsync<CatalogProviderException>(() => GetAsync(service));
        provider.Failure = null;
        await GetAsync(service);

        Assert.Equal(2, provider.Calls);
    }

    private static CatalogGenreService CreateService(
        ICatalogGenreProvider provider,
        TimeProvider clock) =>
        new([provider], new InMemoryCatalogGenreCache(clock));

    private static Task<IReadOnlyList<CatalogGenre>> GetAsync(CatalogGenreService service) =>
        service.GetGenresAsync(CatalogProvider.Tmdb, CatalogContentType.Movie, "pt-BR", default);

    private sealed class CountingGenreProvider : ICatalogGenreProvider
    {
        public int Calls { get; private set; }

        public Exception? Failure { get; set; }

        public CatalogProvider Provider => CatalogProvider.Tmdb;

        public Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
            CatalogContentType contentType,
            string language,
            CancellationToken cancellationToken)
        {
            Calls++;
            return Failure is null
                ? Task.FromResult<IReadOnlyList<CatalogGenre>>([new CatalogGenre(28, "Ação")])
                : Task.FromException<IReadOnlyList<CatalogGenre>>(Failure);
        }
    }
}
