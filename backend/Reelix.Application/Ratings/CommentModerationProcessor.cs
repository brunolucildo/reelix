using Microsoft.Extensions.Logging;

namespace Reelix.Application.Ratings;

public sealed class CommentModerationProcessor(
    IRatingsCommentsRepository repository,
    ICommentSpoilerClassifier classifier,
    ILogger<CommentModerationProcessor> logger)
{
    private const int BatchSize = 5;
    private const int MaxAttempts = 3;

    public async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        var workItems = await repository.ClaimModerationWorkAsync(
            BatchSize,
            DateTimeOffset.UtcNow,
            TimeSpan.FromMinutes(2),
            cancellationToken);
        foreach (var workItem in workItems)
        {
            try
            {
                var classification = await classifier.ClassifyAsync(
                    workItem.Content,
                    cancellationToken);
                await repository.CompleteModerationAsync(
                    workItem,
                    classification,
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (HttpRequestException exception)
            {
                logger.LogWarning(
                    "Spoiler analysis failed for comment {CommentId} with HTTP transport error {ErrorType}.",
                    workItem.CommentId,
                    exception.GetType().Name);
                await RecordFailureAsync(workItem, cancellationToken);
            }
            catch (System.Text.Json.JsonException exception)
            {
                logger.LogWarning(
                    "Spoiler analysis returned an invalid response for comment {CommentId} ({ErrorType}).",
                    workItem.CommentId,
                    exception.GetType().Name);
                await RecordFailureAsync(workItem, cancellationToken);
            }
            catch (TimeoutException exception)
            {
                logger.LogWarning(
                    "Spoiler analysis timed out for comment {CommentId} ({ErrorType}).",
                    workItem.CommentId,
                    exception.GetType().Name);
                await RecordFailureAsync(workItem, cancellationToken);
            }
        }
    }

    private Task RecordFailureAsync(
        ModerationWorkItem workItem,
        CancellationToken cancellationToken) =>
        repository.FailModerationAsync(
            workItem,
            MaxAttempts,
            DateTimeOffset.UtcNow.AddMinutes(1),
            cancellationToken);
}
