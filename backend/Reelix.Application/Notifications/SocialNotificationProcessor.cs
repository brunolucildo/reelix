namespace Reelix.Application.Notifications;

public sealed class SocialNotificationProcessor(INotificationsRepository repository)
{
    public Task<int> ProcessBatchAsync(CancellationToken cancellationToken) =>
        repository.ProcessSocialEventsAsync(100, cancellationToken);
}
