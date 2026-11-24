using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Workers;

/// <summary>
/// Runs one <see cref="ProcessSocialEventsUseCase"/> cycle at a time: quickly again while there was
/// work, slowly when idle, and after a failure it logs and retries later (the events stay queued).
/// </summary>
public sealed class SocialNotificationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<SocialNotificationWorker> logger) : BackgroundService
{
    private static readonly TimeSpan BusyInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan FailureRetryInterval = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                var useCase = scope.ServiceProvider.GetRequiredService<ProcessSocialEventsUseCase>();
                var result = await useCase.ExecuteAsync(new ProcessSocialEventsCommand(), stoppingToken);
                await Task.Delay(result.Processed > 0 ? BusyInterval : IdleInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Social notification batch failed; events remain queued.");
                await Task.Delay(FailureRetryInterval, stoppingToken);
            }
        }
    }
}
