using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Reelix.Application.Catalog;
using Reelix.Application.Ratings;
using Reelix.Domain.Ratings;
using Reelix.Infrastructure.Ratings;

namespace Reelix.UnitTests.Ratings;

public sealed class CommentModerationTests
{
    [Fact]
    public async Task Classifier_ParsesValidStructuredResponse()
    {
        using var client = CreateClient(_ => Task.FromResult(JsonResponse(
            """{"possible_spoiler":true,"confidence":0.97,"reason":"Specific plot event"}""")));
        var classifier = CreateClassifier(client);

        var result = await classifier.ClassifyAsync("A plot comment", CancellationToken.None);

        Assert.True(result.PossibleSpoiler);
        Assert.Equal(0.97m, result.Confidence);
        Assert.Equal("Specific plot event", result.Reason);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("""{"possible_spoiler":"yes","confidence":0.97,"reason":"Invalid type"}""")]
    [InlineData("""{"possible_spoiler":false,"confidence":1.1,"reason":"Out of range"}""")]
    [InlineData("""{"possible_spoiler":false,"confidence":0.5,"reason":""}""")]
    public async Task Classifier_RejectsMalformedStructuredResponse(string content)
    {
        using var client = CreateClient(_ => Task.FromResult(JsonResponse(content)));
        var classifier = CreateClassifier(client);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            classifier.ClassifyAsync("A plot comment", CancellationToken.None));
    }

    [Fact]
    public async Task Classifier_RejectsMalformedProviderEnvelope()
    {
        using var client = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"choices":[]}""", Encoding.UTF8, "application/json"),
        }));
        var classifier = CreateClassifier(client);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            classifier.ClassifyAsync("A plot comment", CancellationToken.None));
    }

    [Fact]
    public async Task Classifier_ReportsProviderHttpErrors()
    {
        using var client = CreateClient(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)));
        var classifier = CreateClassifier(client);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            classifier.ClassifyAsync("A plot comment", CancellationToken.None));

        Assert.Equal(HttpStatusCode.TooManyRequests, exception.StatusCode);
    }

    [Fact]
    public async Task Classifier_TranslatesHttpTimeoutToTimeoutException()
    {
        using var client = CreateClient(_ =>
            Task.FromException<HttpResponseMessage>(new TaskCanceledException("Request timed out.")));
        var classifier = CreateClassifier(client);

        await Assert.ThrowsAsync<TimeoutException>(() =>
            classifier.ClassifyAsync("A plot comment", CancellationToken.None));
    }

    [Fact]
    public async Task Classifier_DoesNotSendRequestsWhenDisabled()
    {
        var handler = new FakeHttpMessageHandler(_ =>
            throw new InvalidOperationException("The disabled classifier must not send requests."));
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://api.openai.com/v1/") };
        var classifier = new OpenAiCommentSpoilerClassifier(
            client,
            Options.Create(new OpenAiSpoilerOptions { Enabled = false, ApiKey = "test-key" }));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            classifier.ClassifyAsync("A plot comment", CancellationToken.None));
    }

    [Fact]
    public async Task Processor_RecordsRetryableFailuresAndCompletesValidResults()
    {
        var failedWork = CreateWorkItem();
        var successfulWork = CreateWorkItem();
        var repository = new FakeRatingsCommentsRepository([failedWork, successfulWork]);
        var classifier = new QueueClassifier(
            new HttpRequestException("Temporary provider failure."),
            new SpoilerClassification(true, 0.9m, "Plot detail"));
        var processor = new CommentModerationProcessor(
            repository,
            classifier,
            NullLogger<CommentModerationProcessor>.Instance);

        await processor.ProcessBatchAsync(CancellationToken.None);

        Assert.Equal([failedWork], repository.FailedItems);
        Assert.Equal([successfulWork], repository.CompletedItems);
        Assert.Equal(3, repository.MaxAttempts);
    }

    private static OpenAiCommentSpoilerClassifier CreateClassifier(
        HttpClient client) =>
        new(
            client,
            Options.Create(new OpenAiSpoilerOptions { Enabled = true, ApiKey = "test-key" }));

    private static HttpClient CreateClient(
        Func<CancellationToken, Task<HttpResponseMessage>> responseFactory) =>
        new(new FakeHttpMessageHandler(responseFactory))
        {
            BaseAddress = new Uri("https://api.openai.com/v1/"),
            Timeout = TimeSpan.FromSeconds(1),
        };

    private static HttpResponseMessage JsonResponse(string classification) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    choices = new[] { new { message = new { content = classification } } },
                }),
                Encoding.UTF8,
                "application/json"),
        };

    private static ModerationWorkItem CreateWorkItem() =>
        new(Guid.NewGuid(), "Review text", 1, Guid.NewGuid());

    private sealed class FakeHttpMessageHandler(
        Func<CancellationToken, Task<HttpResponseMessage>> responseFactory) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            responseFactory(cancellationToken);
    }

    private sealed class QueueClassifier(params object[] responses) : ICommentSpoilerClassifier
    {
        private readonly Queue<object> _responses = new(responses);

        public Task<SpoilerClassification> ClassifyAsync(
            string content,
            CancellationToken cancellationToken)
        {
            var response = _responses.Dequeue();
            return response is Exception exception
                ? Task.FromException<SpoilerClassification>(exception)
                : Task.FromResult((SpoilerClassification)response);
        }
    }

    private sealed class FakeRatingsCommentsRepository(
        IReadOnlyList<ModerationWorkItem> workItems) : IRatingsCommentsRepository
    {
        public Task<OwnRatingsPage> GetOwnRatingsAsync(Guid accountId, int page, int pageSize, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public List<ModerationWorkItem> FailedItems { get; } = [];
        public List<ModerationWorkItem> CompletedItems { get; } = [];
        public int MaxAttempts { get; private set; }

        public Task<bool> CatalogTargetExistsAsync(
            Guid targetId,
            CatalogReferenceType expectedType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> EpisodeBelongsToSeriesAsync(
            Guid episodeId,
            Guid seriesId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RatingSummary> GetRatingSummaryAsync(
            Guid viewerAccountId,
            Guid targetId,
            ReviewTargetType targetType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task UpsertRatingAsync(UserRating rating, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteRatingAsync(
            Guid accountId,
            Guid targetId,
            ReviewTargetType targetType,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task AddCommentAsync(UserComment comment, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<CommentItem?> UpdateCommentAsync(
            Guid accountId,
            Guid commentId,
            string content,
            bool containsSpoiler,
            ReviewAudience audience,
            bool requestAutomatedAnalysis,
            bool automatedAnalysisEnabled,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<bool> DeleteCommentAsync(
            Guid accountId,
            Guid commentId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<PagedComments> GetCommentsAsync(
            Guid viewerAccountId,
            Guid targetId,
            ReviewTargetType targetType,
            int page,
            int pageSize,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<RevealedComment?> RevealCommentAsync(
            Guid viewerAccountId,
            Guid commentId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ModerationWorkItem>> ClaimModerationWorkAsync(
            int batchSize,
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken) =>
            Task.FromResult(workItems);

        public Task CompleteModerationAsync(
            ModerationWorkItem workItem,
            SpoilerClassification classification,
            CancellationToken cancellationToken)
        {
            CompletedItems.Add(workItem);
            return Task.CompletedTask;
        }

        public Task FailModerationAsync(
            ModerationWorkItem workItem,
            int maxAttempts,
            DateTimeOffset retryAfter,
            CancellationToken cancellationToken)
        {
            FailedItems.Add(workItem);
            MaxAttempts = maxAttempts;
            return Task.CompletedTask;
        }
    }
}
