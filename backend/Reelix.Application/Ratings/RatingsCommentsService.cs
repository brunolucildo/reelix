using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Domain.Ratings;

namespace Reelix.Application.Ratings;

public sealed class RatingsCommentsService(
    IUserAccountRepository accounts,
    IRatingsCommentsRepository repository,
    ICommentModerationConfiguration moderationConfiguration)
{
    public async Task<RatingSummary> GetRatingSummaryAsync(
        Guid supabaseUserId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        CancellationToken cancellationToken)
    {
        await ValidateTargetAsync(targetId, targetType, seriesId, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await repository.GetRatingSummaryAsync(
            account.Id,
            targetId,
            targetType,
            cancellationToken);
    }

    public async Task<RatingSummary> UpsertRatingAsync(
        Guid supabaseUserId,
        Guid targetId,
        ReviewTargetType targetType,
        RatingInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        await ValidateTargetAsync(targetId, targetType, input.SeriesId, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        var rating = UserRating.Create(
            account.Id,
            targetId,
            targetType,
            input.SeriesId,
            input.Score,
            input.Audience);
        await repository.UpsertRatingAsync(rating, cancellationToken);
        return await repository.GetRatingSummaryAsync(
            account.Id,
            targetId,
            targetType,
            cancellationToken);
    }

    public async Task DeleteRatingAsync(
        Guid supabaseUserId,
        Guid targetId,
        ReviewTargetType targetType,
        CancellationToken cancellationToken)
    {
        if (targetId == Guid.Empty || !Enum.IsDefined(targetType))
        {
            throw new ArgumentException("Informe um título válido.", nameof(targetId));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        await repository.DeleteRatingAsync(account.Id, targetId, targetType, cancellationToken);
    }

    public async Task<PagedComments> GetCommentsAsync(
        Guid supabaseUserId,
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        ValidatePage(page, pageSize);
        await ValidateTargetAsync(targetId, targetType, seriesId, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await repository.GetCommentsAsync(
            account.Id,
            targetId,
            targetType,
            page,
            pageSize,
            cancellationToken);
    }

    public async Task<CommentItem> CreateCommentAsync(
        Guid supabaseUserId,
        Guid targetId,
        ReviewTargetType targetType,
        CommentInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        await ValidateTargetAsync(targetId, targetType, input.SeriesId, cancellationToken);
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        var comment = UserComment.Create(
            account.Id,
            targetId,
            targetType,
            input.SeriesId,
            input.Content,
            input.ContainsSpoiler,
            input.Audience,
            input.RequestAutomatedAnalysis,
            moderationConfiguration.IsEnabled);
        await repository.AddCommentAsync(comment, cancellationToken);
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

    public async Task<CommentItem?> UpdateCommentAsync(
        Guid supabaseUserId,
        Guid commentId,
        CommentInput input,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (commentId == Guid.Empty)
        {
            throw new ArgumentException("Informe um comentário válido.", nameof(commentId));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await repository.UpdateCommentAsync(
            account.Id,
            commentId,
            input.Content,
            input.ContainsSpoiler,
            input.Audience,
            input.RequestAutomatedAnalysis,
            moderationConfiguration.IsEnabled,
            cancellationToken);
    }

    public async Task<bool> DeleteCommentAsync(
        Guid supabaseUserId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        if (commentId == Guid.Empty)
        {
            throw new ArgumentException("Informe um comentário válido.", nameof(commentId));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await repository.DeleteCommentAsync(account.Id, commentId, cancellationToken);
    }

    public async Task<RevealedComment?> RevealCommentAsync(
        Guid supabaseUserId,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        if (commentId == Guid.Empty)
        {
            throw new ArgumentException("Informe um comentário válido.", nameof(commentId));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await repository.RevealCommentAsync(account.Id, commentId, cancellationToken);
    }

    private async Task ValidateTargetAsync(
        Guid targetId,
        ReviewTargetType targetType,
        Guid? seriesId,
        CancellationToken cancellationToken)
    {
        if (targetId == Guid.Empty || !Enum.IsDefined(targetType))
        {
            throw new ArgumentException("Informe um alvo válido.", nameof(targetId));
        }

        var catalogType = targetType switch
        {
            ReviewTargetType.Movie => CatalogReferenceType.Movie,
            ReviewTargetType.Series => CatalogReferenceType.Series,
            ReviewTargetType.Episode => CatalogReferenceType.Episode,
            _ => throw new ArgumentOutOfRangeException(nameof(targetType)),
        };
        if (!await repository.CatalogTargetExistsAsync(targetId, catalogType, cancellationToken))
        {
            throw new CatalogTargetNotFoundException();
        }

        if (targetType == ReviewTargetType.Episode)
        {
            if (!seriesId.HasValue || seriesId == Guid.Empty ||
                !await repository.EpisodeBelongsToSeriesAsync(
                    targetId,
                    seriesId!.Value,
                    cancellationToken))
            {
                throw new ArgumentException(
                    "O episódio não pertence à série informada.",
                    nameof(seriesId));
            }

        }
        else if (seriesId is not null)
        {
            throw new ArgumentException(
                "Informe o identificador da série somente para episódios.",
                nameof(seriesId));
        }
    }

    private static void ValidatePage(int page, int pageSize)
    {
        if (page is < 1 or > 100_000 || pageSize is < 1 or > 50)
        {
            throw new ArgumentException(
                "A página deve ser positiva e o tamanho deve estar entre 1 e 50.");
        }
    }

}

public sealed class CatalogTargetNotFoundException()
    : Exception("O conteúdo não está disponível no catálogo.");
