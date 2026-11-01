using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications;

public sealed class NotificationDeliveryWorker(
    IServiceScopeFactory scopeFactory,
    IPushRegistrationConfiguration configuration,
    ILogger<NotificationDeliveryWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (configuration.IsEnabled)
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider
                        .GetRequiredService<NotificationDeliveryProcessor>();
                    await processor.ProcessBatchAsync(stoppingToken);
                }

                await Task.Delay(
                    configuration.IsEnabled ? TimeSpan.FromSeconds(10) : TimeSpan.FromMinutes(1),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Expo notification delivery batch failed; work will be retried.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
