using Reelix.Application.Authentication;

namespace Reelix.Application.Notifications;

/// <summary>
/// <c>GET /notifications/preferences</c>: never writes. An account that never saved preferences
/// (or does not exist yet) has every category off, which is also how the workers read a missing row.
/// </summary>
public sealed class GetNotificationPreferencesUseCase(
    IUserAccountRepository accounts,
    INotificationPreferencesStore preferences)
{
    public async Task<GetNotificationPreferencesResult> ExecuteAsync(
        GetNotificationPreferencesQuery query,
        CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(query.SubjectId, cancellationToken);
        var saved = account is null
            ? null
            : await preferences.FindAsync(account.Id, cancellationToken);
        return saved is null
            ? new GetNotificationPreferencesResult(false, false, false)
            : new GetNotificationPreferencesResult(
                saved.SocialEnabled,
                saved.NewEpisodeEnabled,
                saved.PremiereEnabled);
    }
}
