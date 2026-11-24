using Reelix.Application.Authentication;

namespace Reelix.Application.Notifications;

/// <summary><c>PUT /notifications/inbox/{id}/read</c>: marks one notification of the caller as read.</summary>
public sealed class MarkNotificationReadUseCase(
    IUserAccountRepository accounts,
    IUserNotifications notifications,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<MarkNotificationReadResult> ExecuteAsync(
        MarkNotificationReadCommand command,
        CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(command.SubjectId, cancellationToken);
        if (account is null)
        {
            return new MarkNotificationReadResult(false);
        }

        var notification = await notifications.FindOwnedAsync(account.Id, command.NotificationId, cancellationToken);
        if (notification is null)
        {
            return new MarkNotificationReadResult(false);
        }

        notification.MarkRead(clock.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new MarkNotificationReadResult(true);
    }
}
