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
        profile.Update(
            update.DisplayName,
            update.Biography,
            update.ProfileIsPublic,
            update.FavoritesArePublic,
            update.WatchHistoryIsPublic);
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
            savedPreferences?.FavoriteSeriesIds ?? []);
    }

    private static UserProfileData ToData(UserProfile profile) => new(
        profile.DisplayName,
        profile.Biography,
        null,
        profile.ProfileIsPublic,
        profile.FavoritesArePublic,
        profile.WatchHistoryIsPublic);
}