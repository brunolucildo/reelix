using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Lists;
using Reelix.Application.Ratings;
using Reelix.Application.Tracking;
using Reelix.Domain.Users;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Common;

/// <summary>
/// Pins the page limits and the exact error texts of the four paginated flows (spec 0013, R6).
/// The expectations were written against the three private copies of <c>ValidatePage</c> before
/// they moved to a shared component. Limits are intentionally different per flow.
/// </summary>
public sealed class PageLimitBaselineTests
{
    private static readonly Guid Subject = Guid.NewGuid();

    [Theory]
    [InlineData(1, 1)]
    [InlineData(1_000_000, 100)]
    public async Task Lists_AcceptsTheLimits(int page, int pageSize) =>
        await AssertAcceptedAsync(() => ListsService().GetFavoritesAsync(
            Subject, page, pageSize, CancellationToken.None));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1_000_001, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Lists_RejectsValuesOutsideTheLimits(int page, int pageSize)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => ListsService().GetFavoritesAsync(
            Subject, page, pageSize, CancellationToken.None));

        Assert.Equal(typeof(ArgumentException), exception.GetType());
        Assert.Null(exception.ParamName);
        Assert.Equal(
            "Informe uma página entre 1 e 1.000.000 e um limite entre 1 e 100 títulos.",
            exception.Message);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(100_000, 50)]
    public async Task Ratings_AcceptsTheLimits(int page, int pageSize) =>
        await AssertAcceptedAsync(() => RatingsService().GetOwnRatingsAsync(
            Subject, page, pageSize, CancellationToken.None));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(100_001, 1)]
    [InlineData(1, 0)]
    [InlineData(1, 51)]
    public async Task Ratings_RejectsValuesOutsideTheLimits(int page, int pageSize)
    {
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => RatingsService().GetOwnRatingsAsync(
            Subject, page, pageSize, CancellationToken.None));

        Assert.Equal(typeof(ArgumentException), exception.GetType());
        Assert.Null(exception.ParamName);
        Assert.Equal(
            "A página deve ser positiva e o tamanho deve estar entre 1 e 50.",
            exception.Message);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(100_000, 100)]
    public async Task Tracking_AcceptsTheLimits(int page, int pageSize) =>
        await AssertAcceptedAsync(() => TrackingService().GetSeriesAsync(
            Subject, page, pageSize, CancellationToken.None));

    [Theory]
    [InlineData(0, 1)]
    [InlineData(100_001, 1)]
    [InlineData(0, 0)]
    public async Task Tracking_RejectsThePageWithAParameterSpecificError(int page, int pageSize)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TrackingService().GetSeriesAsync(Subject, page, pageSize, CancellationToken.None));

        Assert.Equal("page", exception.ParamName);
        Assert.Equal("A página deve estar entre 1 e 100000. (Parameter 'page')", exception.Message);
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task Tracking_RejectsThePageSizeWithAParameterSpecificError(int page, int pageSize)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            TrackingService().GetSeriesAsync(Subject, page, pageSize, CancellationToken.None));

        Assert.Equal("pageSize", exception.ParamName);
        Assert.Equal(
            "O tamanho da página deve estar entre 1 e 100. (Parameter 'pageSize')",
            exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Catalog_SearchAcceptsThePageLimits(int page)
    {
        var exception = await Assert.ThrowsAsync<CatalogProviderException>(() =>
            CatalogService().SearchAsync(
                CatalogProvider.Tmdb, "matrix", null, page, "pt-BR", CancellationToken.None));

        Assert.Equal("catalog_provider_not_supported", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Catalog_SearchRejectsPagesOutsideTheLimits(int page)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CatalogService().SearchAsync(
                CatalogProvider.Tmdb, "matrix", null, page, "pt-BR", CancellationToken.None));

        Assert.Equal("page", exception.ParamName);
        Assert.Equal("A página deve estar entre 1 e 100. (Parameter 'page')", exception.Message);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    public async Task Catalog_DiscoverAcceptsThePageLimits(int page)
    {
        var exception = await Assert.ThrowsAsync<CatalogProviderException>(() =>
            CatalogService().DiscoverAsync(
                CatalogProvider.Tmdb,
                CatalogContentType.Movie,
                CatalogDiscoveryCategory.Popular,
                [],
                page,
                "pt-BR",
                CancellationToken.None));

        Assert.Equal("catalog_provider_not_supported", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public async Task Catalog_DiscoverRejectsPagesOutsideTheLimits(int page)
    {
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CatalogService().DiscoverAsync(
                CatalogProvider.Tmdb,
                CatalogContentType.Movie,
                CatalogDiscoveryCategory.Popular,
                [],
                page,
                "pt-BR",
                CancellationToken.None));

        Assert.Equal("page", exception.ParamName);
        Assert.Equal("A página deve estar entre 1 e 100. (Parameter 'page')", exception.Message);
    }

    private static UserListsService ListsService() => new(new ReachedAccounts(), null!, null!);

    private static RatingsCommentsService RatingsService() =>
        new(new ReachedAccounts(), null!, null!, null!, FixedTimeProvider.AtDefault());

    private static TrackingService TrackingService() =>
        new(new ReachedAccounts(), null!, null!, FixedTimeProvider.AtDefault());

    private static CatalogService CatalogService() => new([], null!);

    private static async Task AssertAcceptedAsync(Func<Task> call) =>
        await Assert.ThrowsAsync<ValidationPassedException>(call);

    private sealed class ValidationPassedException : Exception;

    /// <summary>Throws as soon as the service gets past its page validation.</summary>
    private sealed class ReachedAccounts : IUserAccountRepository
    {
        public async Task<UserAccount?> FindAsync(
            Guid supabaseUserId,
            CancellationToken cancellationToken) =>
            await EnsureAsync(supabaseUserId, cancellationToken);

        public Task<UserAccount> EnsureAsync(Guid supabaseUserId, CancellationToken cancellationToken) =>
            throw new ValidationPassedException();
    }
}
