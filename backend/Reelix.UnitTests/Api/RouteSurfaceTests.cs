using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Reelix.UnitTests.Api;

/// <summary>
/// Pins the public route surface (method, template, endpoint name, authorization and rate limit
/// policy) of every product route. The baseline was captured from the code before spec 0013 moved
/// anything, so any drift in a later step fails here.
/// </summary>
public sealed class RouteSurfaceTests(AuthenticationApiFactory factory)
    : IClassFixture<AuthenticationApiFactory>
{
    private static readonly string[] Baseline =
    [
        "GET /api/v1/auth/me | GetAuthenticatedAccount | auth | -",
        "GET /api/v1/catalog/genres | GetCatalogGenres | auth | -",
        "GET /api/v1/catalog/search | SearchCatalog | auth | -",
        "GET /api/v1/catalog/discover | DiscoverCatalog | auth | -",
        "GET /api/v1/catalog/{provider}/{contentType}/{externalId} | GetCatalogTitle | auth | -",
        "GET /api/v1/catalog/{provider}/series/{externalSeriesId}/seasons/{seasonNumber:int} | GetCatalogSeason | auth | -",
        "GET /api/v1/catalog/{provider}/series/{externalSeriesId}/seasons | GetCatalogSeasons | auth | -",
        "GET /api/v1/onboarding/ | GetOnboardingPreferences | auth | -",
        "PUT /api/v1/onboarding/progress | SaveOnboardingProgress | auth | -",
        "POST /api/v1/onboarding/complete | CompleteOnboarding | auth | -",
        "GET /api/v1/profile/ | GetOwnProfile | auth | -",
        "GET /api/v1/profile/{accountId:guid} | GetPublicProfile | auth | -",
        "PUT /api/v1/profile/ | UpdateOwnProfile | auth | -",
        "GET /api/v1/tracking/history | GetWatchHistory | auth | -",
        "PUT /api/v1/tracking/movies/{titleId:guid} | MarkMovieWatched | auth | -",
        "GET /api/v1/tracking/movies/{titleId:guid} | GetMovieTrackingStatus | auth | -",
        "DELETE /api/v1/tracking/movies/{titleId:guid} | RemoveWatchedMovie | auth | -",
        "PUT /api/v1/tracking/series/{seriesId:guid}/episodes/{episodeId:guid} | MarkEpisodeWatched | auth | -",
        "DELETE /api/v1/tracking/series/{seriesId:guid}/episodes/{episodeId:guid} | RemoveWatchedEpisode | auth | -",
        "GET /api/v1/tracking/series | GetTrackedSeries | auth | -",
        "GET /api/v1/tracking/series/{seriesId:guid}/progress | GetSeriesProgress | auth | -",
        "GET /api/v1/tracking/series/{seriesId:guid}/seasons/{seasonNumber:int}/progress | GetSeasonProgress | auth | -",
        "GET /api/v1/tracking/series/{seriesId:guid}/seasons/{seasonNumber:int}/episodes/pending | GetPendingEpisodes | auth | -",
        "GET /api/v1/favorites | GetFavorites | auth | -",
        "POST /api/v1/title-status | GetTitleRelationStatuses | auth | -",
        "PUT /api/v1/favorites/{titleId:guid} | AddFavorite | auth | -",
        "DELETE /api/v1/favorites/{titleId:guid} | RemoveFavorite | auth | -",
        "GET /api/v1/watchlist | GetWatchlist | auth | -",
        "PUT /api/v1/watchlist/{titleId:guid} | AddWatchlistTitle | auth | -",
        "DELETE /api/v1/watchlist/{titleId:guid} | RemoveWatchlistTitle | auth | -",
        "GET /api/v1/lists | GetCustomLists | auth | -",
        "POST /api/v1/lists | CreateCustomList | auth | -",
        "GET /api/v1/lists/{listId:guid} | GetCustomList | auth | -",
        "PUT /api/v1/lists/{listId:guid} | UpdateCustomList | auth | -",
        "DELETE /api/v1/lists/{listId:guid} | DeleteCustomList | auth | -",
        "PUT /api/v1/lists/{listId:guid}/titles/{titleId:guid} | AddCustomListTitle | auth | -",
        "DELETE /api/v1/lists/{listId:guid}/titles/{titleId:guid} | RemoveCustomListTitle | auth | -",
        "PUT /api/v1/lists/{listId:guid}/titles/order | ReorderCustomListTitles | auth | -",
        "GET /api/v1/ratings/mine | GetOwnRatings | auth | -",
        "GET /api/v1/ratings/{targetType}/{targetId:guid} | GetRatingSummary | auth | -",
        "PUT /api/v1/ratings/{targetType}/{targetId:guid} | UpsertRating | auth | reviews-write",
        "DELETE /api/v1/ratings/{targetType}/{targetId:guid} | DeleteRating | auth | reviews-write",
        "GET /api/v1/comments/{targetType}/{targetId:guid} | GetComments | auth | -",
        "POST /api/v1/comments/{targetType}/{targetId:guid} | CreateComment | auth | reviews-write",
        "PUT /api/v1/comments/{commentId:guid} | UpdateComment | auth | reviews-write",
        "DELETE /api/v1/comments/{commentId:guid} | DeleteComment | auth | reviews-write",
        "POST /api/v1/comments/{commentId:guid}/reveal | RevealComment | auth | -",
        "GET /api/v1/social/users | SearchSocialUsers | auth | social-search",
        "POST /api/v1/social/friend-requests | SendFriendRequest | auth | social-write",
        "GET /api/v1/social/friend-requests | GetFriendRequests | auth | -",
        "PUT /api/v1/social/friend-requests/{relationshipId:guid}/accept | AcceptFriendRequest | auth | social-write",
        "PUT /api/v1/social/friend-requests/{relationshipId:guid}/decline | DeclineFriendRequest | auth | social-write",
        "DELETE /api/v1/social/friend-requests/{relationshipId:guid} | CancelFriendRequest | auth | social-write",
        "GET /api/v1/social/friends | GetFriends | auth | -",
        "GET /api/v1/social/friends/{accountId:guid}/profile | GetFriendProfile | auth | -",
        "DELETE /api/v1/social/friends/{relationshipId:guid} | RemoveFriend | auth | social-write",
        "GET /api/v1/social/friends/{accountId:guid}/activities | GetFriendActivities | auth | -",
        "GET /api/v1/social/feed | GetSocialFeed | auth | -",
        "GET /api/v1/notifications/preferences | GetNotificationPreferences | auth | -",
        "PUT /api/v1/notifications/preferences | UpdateNotificationPreferences | auth | notifications-write",
        "POST /api/v1/notifications/devices | RegisterNotificationDevice | auth | notifications-write",
        "DELETE /api/v1/notifications/devices/{deviceId:guid} | DeactivateNotificationDevice | auth | notifications-write",
        "GET /api/v1/notifications/inbox | GetNotificationInbox | auth | -",
        "GET /api/v1/notifications/inbox/unread-count | GetNotificationUnreadCount | auth | -",
        "PUT /api/v1/notifications/inbox/{notificationId:guid}/read | MarkNotificationRead | auth | -",
        "GET /api/v1/gamification/dashboard | GetGamificationDashboard | auth | -",
        "PUT /api/v1/gamification/preferences | UpdateGamificationPreferences | auth | social-write",
        "GET /api/v1/gamification/statistics | GetGamificationStatistics | auth | -",
        "GET /api/v1/gamification/users/{accountId:guid}/badges | GetPublicGamificationBadges | auth | -",
        "GET /api/v1/gamification/friends/ranking | GetMonthlyFriendsRanking | auth | -",
    ];

    [Fact]
    public void ProductRoutes_MatchTheBaselineSurface()
    {
        var actual = CaptureProductRoutes(factory.Services);

        Assert.Equal(Baseline.Order(StringComparer.Ordinal), actual.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ProductRoutes_AreAuthenticatedExceptTheOnesListedAsAnonymous()
    {
        Assert.All(
            CaptureProductRoutes(factory.Services),
            route => Assert.Contains("| auth |", route));
    }

    [Fact]
    public void AuthenticatedAccountRoute_KeepsItsOpenApiName()
    {
        var endpoint = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(candidate => candidate.RoutePattern.RawText == "/api/v1/auth/me");

        Assert.Equal(
            "GetAuthenticatedAccount",
            endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName);
        Assert.Equal(
            ["Authentication"],
            endpoint.Metadata.GetMetadata<ITagsMetadata>()?.Tags);
    }

    internal static string[] CaptureProductRoutes(IServiceProvider services) =>
        services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(endpoint => endpoint.RoutePattern.RawText is { } text &&
                text.StartsWith("/api/v1/", StringComparison.Ordinal) &&
                !text.StartsWith("/api/v1/dev/", StringComparison.Ordinal))
            .Select(endpoint =>
            {
                var methods = endpoint.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [];
                var name = endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName;
                var authorized = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any();
                var policy = endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName;
                return $"{string.Join(",", methods)} {endpoint.RoutePattern.RawText} | {name} | " +
                    $"{(authorized ? "auth" : "anon")} | {policy ?? "-"}";
            })
            .ToArray();
}
