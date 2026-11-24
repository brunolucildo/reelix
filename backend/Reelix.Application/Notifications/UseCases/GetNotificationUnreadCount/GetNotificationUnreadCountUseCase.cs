using Reelix.Application.Authentication;

namespace Reelix.Application.Notifications;

/// <summary><c>GET /notifications/inbox/unread-count</c>: never writes; no account means nothing unread.</summary>
public sealed class GetNotificationUnreadCountUseCase(
    IUserAccountRepository accounts,
    IUserNotifications notifications)
{
    public async Task<GetNotificationUnreadCountResult> ExecuteAsync(
        GetNotificationUnreadCountQuery query,
        CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(query.SubjectId, cancellationToken);
        return new GetNotificationUnreadCountResult(
            account is null
                ? 0
                : await notifications.CountUnreadAsync(account.Id, cancellationToken));
    }
}
