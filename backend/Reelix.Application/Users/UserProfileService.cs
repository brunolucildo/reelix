using Reelix.Application.Authentication;
using Reelix.Domain.Users;

namespace Reelix.Application.Users;

public sealed class UserProfileService(
    IUserAccountRepository accounts,
    IUserProfileRepository profiles,
    IUserPreferencesRepository preferences)
{
    public async Task<UserProfileData> GetAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return ToData(await profiles.GetOrCreateAsync(account.Id, cancellationToken));
    }

    public async Task<UserProfileData> UpdateAsync(
        Guid supabaseUserId,
        UpdateUserProfile update,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        var profile = await profiles.GetOrCreateAsync(account.Id, cancellationToken);
        if (update.AvatarKey is not null) profile.SetAvatar(update.AvatarKey);
        profile.Update(
            update.DisplayName,
            update.Biography,
            update.ProfileIsPublic,
            update.FavoritesArePublic,
            update.WatchHistoryIsPublic,
            update.IsDiscoverable ?? profile.IsDiscoverable,
            update.WatchHistorySharedWithFriends ?? profile.WatchHistorySharedWithFriends,
            update.FavoritesSharedWithFriends ?? profile.FavoritesSharedWithFriends,
            update.RatingsSharedWithFriends ?? profile.RatingsSharedWithFriends);
        await profiles.SaveAsync(profile, cancellationToken);
        return ToData(profile);
    }

    public async Task<PublicUserProfileData?> GetPublicAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var profile = await profiles.GetPublicAsync(accountId, cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var savedPreferences = profile.FavoritesArePublic
            ? await preferences.GetExistingAsync(accountId, cancellationToken)
            : null;
        return new PublicUserProfileData(
            profile.DisplayName,
            profile.Biography,
            null,
            savedPreferences?.FavoriteMovieIds ?? [],
            savedPreferences?.FavoriteSeriesIds ?? [],
            profile.AvatarKey);
    }

    private static UserProfileData ToData(UserProfile profile) => new(
        profile.DisplayName,
        profile.Biography,
        null,
        profile.ProfileIsPublic,
        profile.FavoritesArePublic,
        profile.WatchHistoryIsPublic,
        profile.IsDiscoverable,
        profile.WatchHistorySharedWithFriends,
        profile.FavoritesSharedWithFriends,
        profile.RatingsSharedWithFriends,
        profile.AvatarKey);
}