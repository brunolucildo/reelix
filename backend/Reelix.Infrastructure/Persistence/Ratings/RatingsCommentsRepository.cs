using Microsoft.EntityFrameworkCore;
using System.Data;
using Reelix.Application.Catalog;
using Reelix.Application.Ratings;
using Reelix.Domain.Catalog;
using Reelix.Domain.Ratings;

namespace Reelix.Infrastructure.Persistence.Ratings;

public sealed class RatingsCommentsRepository(ReelixDbContext dbContext)
    : IRatingsCommentsRepository
{
    public Task<bool> CatalogTargetExistsAsync(
        Guid targetId,
        CatalogReferenceType expectedType,
        CancellationToken cancellationToken) =>
        dbContext.ExternalCatalogReferences.AnyAsync(
            reference =>
                reference.Id == targetId &&
                reference.ContentType == expectedType.ToString().ToLowerInvariant(),
            cancellationToken);

    public Task<bool> EpisodeBelongsToSeriesAsync(
        Guid episodeId,
        Guid seriesId,
        CancellationToken cancellationToken) =>
        dbContext.CatalogEpisodeSeries.AnyAsync(
            link => link.EpisodeId == episodeId && link.SeriesId == seriesId,
            cancellationToken);

    public async Task<RatingSummary> GetRatingSummaryAsync(
        Guid viewerAccountId,
        Guid targetId,
        ReviewTargetType targetType,
        CancellationToken cancellationToken)
    {
        var publicRatings = dbContext.UserRatings.Where(rating =>
            rating.TargetId == targetId &&
            rating.TargetType == targetType &&
            rating.Audience == ReviewAudience.Public);
        var count = await publicRatings.CountAsync(cancellationToken);
        decimal? average = null;
        if (count > 0)
        {
            average = await publicRatings.AverageAsync(
                rating => rating.Value,
                cancellationToken);
        }
        var ownRating = await dbContext.UserRatings
            .Where(rating =>
                rating.AccountId == viewerAccountId &&
                rating.TargetId == targetId &&
                rating.TargetType == targetType)
            .Select(rating => new OwnRating(
                rating.Id,
                rating.Value,
                rating.Audience,
                rating.CreatedAt,
                rating.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken);

        return new RatingSummary(targetId, targetType, average, count, ownRating);
    }

    public Task UpsertRatingAsync(
        UserRating rating,
        CancellationToken cancellationToken) =>
        dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO public.user_ratings
                (id, account_id, target_id, target_type, series_id, score, audience, created_at, updated_at)
            VALUES
                ({rating.Id}, {rating.AccountId}, {rating.TargetId},
                 {rating.TargetType.ToString().ToLowerInvariant()}, {rating.SeriesId},
                 {rating.Value}, {rating.Audience.ToString().ToLowerInvariant()},
                 {rating.CreatedAt}, {rating.UpdatedAt})
            ON CONFLICT (account_id, target_id)
            DO UPDATE SET
                score = EXCLUDED.score,
                audience = EXCLUDED.audience,
                updated_at = EXCLUDED.updated_at
            """,
            cancellationToken);

    public async Task DeleteRatingAsync(
        Guid accountId,
        Guid targetId,
        ReviewTargetType targetType,
        CancellationToken cancellationToken)
    {
        await dbContext.UserRatings
            .Where(rating =>
                rating.AccountId == accountId &&
                rating.TargetId == targetId &&
                rating.TargetType == targetType)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task AddCommentAsync(
        UserComment comment,
        CancellationToken cancellationToken)
    {
        dbContext.UserComments.Add(comment);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CommentItem?> UpdateCommentAsync(
        Guid accountId,
        Guid commentId,
        string content,
        bool containsSpoiler,
        ReviewAudience audience,
        bool requestAutomatedAnalysis,
        bool automatedAnalysisEnabled,
        CancellationToken cancellationToken)
    {
        var comment = await dbContext.UserComments.SingleOrDefaultAsync(
            item => item.Id == commentId && item.AccountId == accountId,
            cancellationToken);
        if (comment is null)
        {
            return null;
        }

        comment.Update(
            content,
            containsSpoiler,
            audience,
            requestAutomatedAnalysis,
            automatedAnalysisEnabled);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CommentItem(
            comment.Id,
            comment.TargetId,
            comment.TargetType,
            comment.SeriesId,
            "Você",
            comment.ContainsSpoiler || comment.PossibleSpoiler ? null : comment.Content,
            comment.ContainsSpoiler,
            comment.PossibleSpoiler,
            comment.Audience,
            comment.ModerationStatus,
            true,
            comment.CreatedAt,
            comment.UpdatedAt);
    }

    public async Task<bool> DeleteCommentAsync(
        Guid accountId,
        Guid commentId,
        CancellationToken cancellationToken) =>
        await dbContext.UserComments
            .Where(comment => comment.Id == commentId && comment.AccountId == accountId)
            .ExecuteDeleteAsync(cancellationToken) > 0;

    public async Task<PagedComments> GetCommentsAsync(
        Guid viewerAccountId,
        Guid targetId,
        ReviewTargetType targetType,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var comments =
            from comment in dbContext.UserComments
            join profile in dbContext.UserProfiles
                on comment.AccountId equals profile.AccountId into profiles
            from profile in profiles.DefaultIfEmpty()
            where comment.TargetId == targetId &&
                comment.TargetType == targetType &&
                ((comment.ModerationStatus == CommentModerationStatus.Published &&
                  comment.Audience == ReviewAudience.Public) ||
                 comment.AccountId == viewerAccountId)
            select new { Comment = comment, Profile = profile };

        var totalCount = await comments.CountAsync(cancellationToken);
        var items = await comments
            .OrderByDescending(item => item.Comment.CreatedAt)
            .ThenByDescending(item => item.Comment.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(item => new CommentItem(
                item.Comment.Id,
                item.Comment.TargetId,
                item.Comment.TargetType,
                item.Comment.SeriesId,
                item.Comment.AccountId == viewerAccountId
                    ? (item.Profile != null && item.Profile.DisplayName != null
                        ? item.Profile.DisplayName
                        : "Você")
                    : (item.Profile != null && item.Profile.ProfileIsPublic &&
                       item.Profile.DisplayName != null
                        ? item.Profile.DisplayName
                        : "Usuário Reelix"),
                item.Comment.ContainsSpoiler || item.Comment.PossibleSpoiler
                    ? null
                    : item.Comment.Content,
                item.Comment.ContainsSpoiler,
                item.Comment.PossibleSpoiler,
                item.Comment.Audience,
                item.Comment.ModerationStatus,
                item.Comment.AccountId == viewerAccountId,
                item.Comment.CreatedAt,
                item.Comment.UpdatedAt))
            .ToArrayAsync(cancellationToken);

        return new PagedComments(items, page, pageSize, totalCount);
    }

    public async Task<RevealedComment?> RevealCommentAsync(
        Guid viewerAccountId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        var comment = await dbContext.UserComments
            .Where(item =>
                item.Id == commentId &&
                item.ModerationStatus == CommentModerationStatus.Published &&
                (item.Audience == ReviewAudience.Public || item.AccountId == viewerAccountId))
            .Select(item => new RevealedComment(item.Id, item.Content))
            .SingleOrDefaultAsync(cancellationToken);
        return comment;
    }

    public async Task<IReadOnlyList<ModerationWorkItem>> ClaimModerationWorkAsync(
        int batchSize,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        if (batchSize is < 1 or > 50 || leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize));
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.ReadCommitted,
            cancellationToken);
        var candidates = await dbContext.UserComments
            .FromSqlInterpolated(
                $"""
                SELECT * FROM public.user_comments
                WHERE moderation_status = 'pending'
                  AND automated_analysis_requested = TRUE
                  AND (moderation_lease_until IS NULL OR moderation_lease_until <= {now})
                ORDER BY created_at, id
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(cancellationToken);

        var workItems = new List<ModerationWorkItem>(candidates.Count);
        foreach (var comment in candidates)
        {
            var leaseId = Guid.NewGuid();
            if (comment.TryClaimModeration(leaseId, now.Add(leaseDuration), now))
            {
                workItems.Add(new ModerationWorkItem(
                    comment.Id,
                    comment.Content,
                    comment.ModerationVersion,
                    leaseId));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return workItems;
    }

    public async Task CompleteModerationAsync(
        ModerationWorkItem workItem,
        SpoilerClassification classification,
        CancellationToken cancellationToken)
    {
        if (classification.Confidence is < 0 or > 1 ||
            string.IsNullOrWhiteSpace(classification.Reason) ||
            classification.Reason.Length > 200)
        {
            throw new ArgumentException("A classificação de spoiler é inválida.", nameof(classification));
        }

        var comment = await dbContext.UserComments.SingleOrDefaultAsync(
            item => item.Id == workItem.CommentId,
            cancellationToken);
        if (comment is null)
        {
            return;
        }

        comment.CompleteModeration(
            workItem.Version,
            workItem.LeaseId,
            classification.PossibleSpoiler);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task FailModerationAsync(
        ModerationWorkItem workItem,
        int maxAttempts,
        DateTimeOffset retryAfter,
        CancellationToken cancellationToken)
    {
        var comment = await dbContext.UserComments.SingleOrDefaultAsync(
            item => item.Id == workItem.CommentId,
            cancellationToken);
        if (comment is null)
        {
            return;
        }

        comment.RecordModerationFailure(
            workItem.Version,
            workItem.LeaseId,
            maxAttempts,
            retryAfter);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
