using Microsoft.AspNetCore.Mvc;
using Reelix.Application.Users;

namespace Reelix.Api.Users;

public static class ProfileEndpoints
{
    public static RouteGroupBuilder MapProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/profile")
            .WithTags("Profile")
            .RequireAuthorization();

        group.MapGet("", Get)
            .WithName("GetOwnProfile")
            .Produces<UserProfileData>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/{accountId:guid}", GetPublic)
            .WithName("GetPublicProfile")
            .Produces<PublicUserProfileData>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        group.MapPut("", Update)
            .WithName("UpdateOwnProfile")
            .Produces<UserProfileData>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> Get(
        HttpContext context,
        UserProfileService profiles,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await profiles.GetAsync(userId, cancellationToken));
    }

    private static async Task<IResult> Update(
        HttpContext context,
        UserProfileService profiles,
        UpdateUserProfile update,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await profiles.UpdateAsync(userId, update, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid profile.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> GetPublic(
        HttpContext context,
        UserProfileService profiles,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out _))
        {
            return Results.Unauthorized();
        }

        var profile = await profiles.GetPublicAsync(accountId, cancellationToken);
        return profile is null ? Results.NotFound() : Results.Ok(profile);
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out userId) && userId != Guid.Empty;
}