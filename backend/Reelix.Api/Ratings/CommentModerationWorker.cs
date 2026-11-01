using Reelix.Application.Ratings;

namespace Reelix.Api.Ratings;

public sealed class CommentModerationWorker(
    IServiceScopeFactory scopeFactory,
    ICommentModerationConfiguration configuration,
    ILogger<CommentModerationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (configuration.IsEnabled)
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var processor = scope.ServiceProvider
                        .GetRequiredService<CommentModerationProcessor>();
                    await processor.ProcessBatchAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Comment moderation batch failed; queued comments will be retried.");
                }
            }

            await Task.Delay(
                configuration.IsEnabled
                    ? TimeSpan.FromSeconds(30)
                    : TimeSpan.FromMinutes(1),
                stoppingToken);
        }
    }
}
