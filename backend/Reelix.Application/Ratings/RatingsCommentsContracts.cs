using Reelix.Domain.Ratings;

namespace Reelix.Application.Ratings;

public sealed record RatingInput(
    decimal Score,
    ReviewAudience Audience,
    Guid? SeriesId = null);

public sealed record OwnRating(
    Guid Id,
    decimal Score,
    ReviewAudience Audience,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record RatingSummary(
    Guid TargetId,
    ReviewTargetType TargetType,
    decimal? AverageScore,
    int PublicCount,
    OwnRating? MyRating);

public sealed record CommentInput(
    string Content,
    bool ContainsSpoiler,
    ReviewAudience Audience,
    bool RequestAutomatedAnalysis,
    Guid? SeriesId = null);

public sealed record CommentItem(
    Guid Id,
    Guid TargetId,
    ReviewTargetType TargetType,
    Guid? SeriesId,
    string AuthorName,
    string? Content,
    bool ContainsSpoiler,
    bool PossibleSpoiler,
    ReviewAudience Audience,
    CommentModerationStatus ModerationStatus,
    bool IsOwn,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PagedComments(
    IReadOnlyList<CommentItem> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record RevealedComment(Guid Id, string Content);

public sealed record ModerationWorkItem(
    Guid CommentId,
    string Content,
    int Version,
    Guid LeaseId);

public sealed record SpoilerClassification(
    bool PossibleSpoiler,
    decimal Confidence,
    string Reason);

public interface IRatingsCommentsRepository
{
    Task<bool> CatalogTargetExistsAsync(
        Guid targetId,
        Catalog.CatalogReferenceType expectedType,
        CancellationToken cancellationToken);

    Task<bool> EpisodeBelongsToSeriesAsync(
        Guid episodeId,
        Guid seriesId,
        CancellationToken cancellationToken);

    Task<RatingSummary> GetRatingSummaryAsync(
        Guid viewerAccountId,
        Guid targetId,
        ReviewTargetType targetType,
        CancellationToken cancellationToken);

    Task UpsertRatingAsync(UserRating rating, CancellationToken cancellationToken);

    Task DeleteRatingAsync(
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        CancellationToken cancellationToken);

    Task AddCommentAsync(UserComment comment, CancellationToken cancellationToken);

    Task<CommentItem?> UpdateCommentAsync(
        Guid accountId,
        Guid commentId,
        string content,
        bool containsSpoiler,
        ReviewAudience audience,
        bool requestAutomatedAnalysis,
        bool automatedAnalysisEnabled,
        CancellationToken cancellationToken);

    Task<bool> DeleteCommentAsync(
        Guid accountId,
        Guid commentId,
        CancellationToken cancellationToken);

    Task<PagedComments> GetCommentsAsync(
        Guid viewerAccountId,
        Guid targetId,
        ReviewTargetType targetType,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<RevealedComment?> RevealCommentAsync(
        Guid viewerAccountId,
        Guid commentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ModerationWorkItem>> ClaimModerationWorkAsync(
        int batchSize,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task CompleteModerationAsync(
        ModerationWorkItem workItem,
        SpoilerClassification classification,
        CancellationToken cancellationToken);

    Task FailModerationAsync(
        ModerationWorkItem workItem,
        int maxAttempts,
        DateTimeOffset retryAfter,
        CancellationToken cancellationToken);
}

public interface ICommentModerationConfiguration
{
    bool IsEnabled { get; }
}

public interface ICommentSpoilerClassifier
{
    Task<SpoilerClassification> ClassifyAsync(
        string content,
        CancellationToken cancellationToken);
}
