using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Workers;

/// <summary>
/// Runs one <see cref="DeliverPushNotificationsUseCase"/> cycle at a time while push is enabled
/// (every 10 seconds), idles for a minute while it is off, and after a failure it logs and
/// retries later (the work stays queued).
/// </summary>
public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IPushRegistrationConfiguration configuration,
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    private static readonly TimeSpan EnabledInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DisabledInterval = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan FailureRetryInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (configuration.IsEnabled)
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var useCase = scope.ServiceProvider.GetRequiredService<DeliverPushNotificationsUseCase>();
                    await useCase.ExecuteAsync(new DeliverPushNotificationsCommand(), stoppingToken);
                }

                await Task.Delay(configuration.IsEnabled ? EnabledInterval : DisabledInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Expo notification delivery batch failed; work will be retried.");
                await Task.Delay(FailureRetryInterval, stoppingToken);
            }
        }
    }
}
