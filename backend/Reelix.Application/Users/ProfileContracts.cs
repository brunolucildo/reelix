namespace Reelix.Application.Users;

public sealed record UserProfileData(
    string? DisplayName,
    string? Biography,
    string? AvatarUrl,
    bool ProfileIsPublic,
    bool FavoritesArePublic,
    bool WatchHistoryIsPublic,
    bool IsDiscoverable = false,
    bool WatchHistorySharedWithFriends = false,
    bool FavoritesSharedWithFriends = false,
    bool RatingsSharedWithFriends = false);

public sealed record UpdateUserProfile(
    string? DisplayName,
    string? Biography,
    bool ProfileIsPublic,
    bool FavoritesArePublic,
    bool WatchHistoryIsPublic,
    bool? IsDiscoverable = null,
    bool? WatchHistorySharedWithFriends = null,
    bool? FavoritesSharedWithFriends = null,
    bool? RatingsSharedWithFriends = null);

public sealed record PublicUserProfileData(
    string? DisplayName,
    string? Biography,
    string? AvatarUrl,
    IReadOnlyList<Guid> FavoriteMovieIds,
    IReadOnlyList<Guid> FavoriteSeriesIds);

public interface IUserProfileRepository
{
    Task<Reelix.Domain.Users.UserProfile> GetOrCreateAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<Reelix.Domain.Users.UserProfile?> GetPublicAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task SaveAsync(
        Reelix.Domain.Users.UserProfile profile,
        CancellationToken cancellationToken);
}