using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications;

public sealed class SocialNotificationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SocialNotificationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var processor = scope.ServiceProvider
                    .GetRequiredService<SocialNotificationProcessor>();
                var processed = await processor.ProcessBatchAsync(stoppingToken);
                await Task.Delay(
                    processed > 0 ? TimeSpan.FromSeconds(1) : TimeSpan.FromSeconds(15),
                    stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Social notification batch failed; events remain queued.");
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
    }
}
