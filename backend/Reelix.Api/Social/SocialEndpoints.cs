using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Reelix.Application.Social;
using Reelix.Domain.Social;

namespace Reelix.Api.Social;

public sealed record CreateFriendRequest(Guid AccountId);

public sealed record SocialActionResult(Guid RelationshipId, FriendRelationshipStatus Status);

public static class SocialEndpoints
{
    public static RouteGroupBuilder MapSocialEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/social")
            .WithTags("Social")
            .RequireAuthorization();

        group.MapGet("/users", SearchUsers)
            .WithName("SearchSocialUsers")
            .RequireRateLimiting("social-search")
            .Produces<IReadOnlyList<SocialUser>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapPost("/friend-requests", SendFriendRequest)
            .WithName("SendFriendRequest")
            .RequireRateLimiting("social-write")
            .Produces<SocialActionResult>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapGet("/friend-requests", GetRequests)
            .WithName("GetFriendRequests")
            .Produces<SocialPage<FriendRequest>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/friend-requests/{relationshipId:guid}/accept", (HttpContext context, SocialService service, Guid relationshipId, CancellationToken token) =>
                Respond(context, service, relationshipId, true, token))
            .WithName("AcceptFriendRequest")
            .RequireRateLimiting("social-write");
        group.MapPut("/friend-requests/{relationshipId:guid}/decline", (HttpContext context, SocialService service, Guid relationshipId, CancellationToken token) =>
                Respond(context, service, relationshipId, false, token))
            .WithName("DeclineFriendRequest")
            .RequireRateLimiting("social-write");
        group.MapDelete("/friend-requests/{relationshipId:guid}", CancelFriendRequest)
            .WithName("CancelFriendRequest")
            .RequireRateLimiting("social-write");
        group.MapGet("/friends", GetFriends)
            .WithName("GetFriends")
            .Produces<SocialPage<FriendItem>>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/friends/{accountId:guid}/profile", GetFriendProfile)
            .WithName("GetFriendProfile")
            .Produces<FriendProfile>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapDelete("/friends/{relationshipId:guid}", RemoveFriend)
            .WithName("RemoveFriend")
            .RequireRateLimiting("social-write");
        group.MapGet("/friends/{accountId:guid}/activities", GetFriendActivities)
            .WithName("GetFriendActivities")
            .Produces<SocialActivityPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/feed", GetFeed)
            .WithName("GetSocialFeed")
            .Produces<SocialActivityPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> SearchUsers(
        HttpContext context,
        SocialService service,
        string query,
        int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.SearchUsersAsync(
                userId,
                query,
                limit ?? 20,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Busca inválida.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> SendFriendRequest(
        HttpContext context,
        SocialService service,
        CreateFriendRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var relationship = await service.RequestFriendshipAsync(
                userId,
                request.AccountId,
                cancellationToken);
            return Results.Created(
                "/api/v1/social/friend-requests",
                new SocialActionResult(relationship.Id, relationship.Status));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Solicitação inválida.",
                detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A solicitação não pode ser enviada.",
                detail: exception.Message);
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> Respond(
        HttpContext context,
        SocialService service,
        Guid relationshipId,
        bool accept,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var relationship = await service.RespondToRequestAsync(
                userId,
                relationshipId,
                accept,
                cancellationToken);
            return relationship is null
                ? Results.NotFound()
                : Results.Ok(new SocialActionResult(relationship.Id, relationship.Status));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A solicitação não pode ser respondida.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> CancelFriendRequest(
        HttpContext context,
        SocialService service,
        Guid relationshipId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var relationship = await service.CancelRequestAsync(userId, relationshipId, cancellationToken);
            return relationship is null
                ? Results.NotFound()
                : Results.Ok(new SocialActionResult(relationship.Id, relationship.Status));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A solicitação não pode ser cancelada.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> RemoveFriend(
        HttpContext context,
        SocialService service,
        Guid relationshipId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var relationship = await service.RemoveFriendAsync(userId, relationshipId, cancellationToken);
            return relationship is null
                ? Results.NotFound()
                : Results.Ok(new SocialActionResult(relationship.Id, relationship.Status));
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "A amizade não pode ser removida.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> GetFriends(
        HttpContext context,
        SocialService service,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetFriendsAsync(
                userId,
                cursor,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Paginação inválida.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> GetFriendProfile(
        HttpContext context,
        SocialService service,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        var profile = await service.GetFriendProfileAsync(userId, accountId, cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static async Task<IResult> GetRequests(
        HttpContext context,
        SocialService service,
        string? direction,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        if (direction is not (null or "incoming" or "outgoing"))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Direção inválida.",
                detail: "Use incoming ou outgoing.");
        }

        try
        {
            var requests = await service.GetRequestsAsync(
                userId,
                incoming: direction != "outgoing",
                cursor,
                cancellationToken);
            return Results.Ok(requests);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Paginação inválida.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> GetFeed(
        HttpContext context,
        SocialService service,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetActivityFeedAsync(userId, cursor, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cursor inválido.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> GetFriendActivities(
        HttpContext context,
        SocialService service,
        Guid accountId,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetActivityFeedAsync(userId, cursor, cancellationToken, accountId));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cursor inválido.",
                detail: exception.Message);
        }
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirstValue("sub"), out userId) && userId != Guid.Empty;
}
