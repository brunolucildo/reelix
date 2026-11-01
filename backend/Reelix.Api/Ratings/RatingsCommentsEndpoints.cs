using Reelix.Application.Ratings;
using Reelix.Domain.Ratings;

namespace Reelix.Api.Ratings;

public static class RatingsCommentsEndpoints
{
    public static RouteGroupBuilder MapRatingsCommentsEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1")
            .WithTags("Ratings and comments")
            .RequireAuthorization();

        group.MapGet("/ratings/{targetType}/{targetId:guid}", GetRating)
            .WithName("GetRatingSummary")
            .WithSummary("Get public rating statistics and the caller's own rating.")
            .Produces<RatingSummary>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("/ratings/{targetType}/{targetId:guid}", PutRating)
            .WithName("UpsertRating")
            .WithSummary("Create or update the authenticated user's rating.")
            .RequireRateLimiting("reviews-write")
            .Produces<RatingSummary>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapDelete("/ratings/{targetType}/{targetId:guid}", DeleteRating)
            .WithName("DeleteRating")
            .WithSummary("Delete the authenticated user's rating.")
            .RequireRateLimiting("reviews-write")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        group.MapGet("/comments/{targetType}/{targetId:guid}", GetComments)
            .WithName("GetComments")
            .WithSummary("Get public comments and the caller's own comments for a catalog target.")
            .Produces<PagedComments>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPost("/comments/{targetType}/{targetId:guid}", CreateComment)
            .WithName("CreateComment")
            .WithSummary("Create a comment for a catalog target.")
            .RequireRateLimiting("reviews-write")
            .Produces<CommentItem>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapPut("/comments/{commentId:guid}", UpdateComment)
            .WithName("UpdateComment")
            .WithSummary("Update one of the authenticated user's comments.")
            .RequireRateLimiting("reviews-write")
            .Produces<CommentItem>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapDelete("/comments/{commentId:guid}", DeleteComment)
            .WithName("DeleteComment")
            .WithSummary("Delete one of the authenticated user's comments.")
            .RequireRateLimiting("reviews-write")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapPost("/comments/{commentId:guid}/reveal", RevealComment)
            .WithName("RevealComment")
            .WithSummary("Reveal a published comment after rechecking its audience.")
            .Produces<RevealedComment>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<IResult> GetRating(
        HttpContext context,
        RatingsCommentsService service,
        string targetType,
        Guid targetId,
        Guid? seriesId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseTargetType(targetType, out var parsedTargetType))
        {
            return InvalidTargetType();
        }

        return await ExecuteAsync(() => service.GetRatingSummaryAsync(
            userId,
            targetId,
            parsedTargetType,
            seriesId,
            cancellationToken));
    }

    private static async Task<IResult> PutRating(
        HttpContext context,
        RatingsCommentsService service,
        string targetType,
        Guid targetId,
        RatingInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseTargetType(targetType, out var parsedTargetType))
        {
            return InvalidTargetType();
        }

        return await ExecuteAsync(() => service.UpsertRatingAsync(
            userId,
            targetId,
            parsedTargetType,
            input,
            cancellationToken));
    }

    private static async Task<IResult> DeleteRating(
        HttpContext context,
        RatingsCommentsService service,
        string targetType,
        Guid targetId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseTargetType(targetType, out var parsedTargetType))
        {
            return InvalidTargetType();
        }

        await service.DeleteRatingAsync(userId, targetId, parsedTargetType, cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> GetComments(
        HttpContext context,
        RatingsCommentsService service,
        string targetType,
        Guid targetId,
        Guid? seriesId,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseTargetType(targetType, out var parsedTargetType))
        {
            return InvalidTargetType();
        }

        return await ExecuteAsync(() => service.GetCommentsAsync(
            userId,
            targetId,
            parsedTargetType,
            seriesId,
            page,
            pageSize,
            cancellationToken));
    }

    private static async Task<IResult> CreateComment(
        HttpContext context,
        RatingsCommentsService service,
        string targetType,
        Guid targetId,
        CommentInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!TryParseTargetType(targetType, out var parsedTargetType))
        {
            return InvalidTargetType();
        }

        try
        {
            var comment = await service.CreateCommentAsync(
                userId,
                targetId,
                parsedTargetType,
                input,
                cancellationToken);
            return Results.Created(
                $"/api/v1/comments/{targetType}/{targetId}",
                comment);
        }
        catch (ArgumentException exception)
        {
            return InvalidRequest(exception);
        }
        catch (CatalogTargetNotFoundException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conteúdo não encontrado.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> UpdateComment(
        HttpContext context,
        RatingsCommentsService service,
        Guid commentId,
        CommentInput input,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var comment = await service.UpdateCommentAsync(
                userId,
                commentId,
                input,
                cancellationToken);
            return comment is null ? Results.NotFound() : Results.Ok(comment);
        }
        catch (ArgumentException exception)
        {
            return InvalidRequest(exception);
        }
    }

    private static async Task<IResult> DeleteComment(
        HttpContext context,
        RatingsCommentsService service,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!await service.DeleteCommentAsync(userId, commentId, cancellationToken))
        {
            return Results.NotFound();
        }

        return Results.NoContent();
    }

    private static async Task<IResult> RevealComment(
        HttpContext context,
        RatingsCommentsService service,
        Guid commentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        var comment = await service.RevealCommentAsync(userId, commentId, cancellationToken);
        return comment is null ? Results.NotFound() : Results.Ok(comment);
    }

    private static async Task<IResult> ExecuteAsync<T>(Func<Task<T>> action)
    {
        try
        {
            return Results.Ok(await action());
        }
        catch (ArgumentException exception)
        {
            return InvalidRequest(exception);
        }
        catch (CatalogTargetNotFoundException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Conteúdo não encontrado.",
                detail: exception.Message);
        }
    }

    private static IResult InvalidRequest(ArgumentException exception) =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Solicitação inválida.",
            detail: exception.ToUserMessage());

    private static IResult InvalidTargetType() =>
        Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Tipo de conteúdo inválido.",
            detail: "Use movie, series ou episode.");

    private static bool TryParseTargetType(string value, out ReviewTargetType targetType) =>
        Enum.TryParse(value, ignoreCase: true, out targetType) &&
        Enum.IsDefined(targetType);

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}
