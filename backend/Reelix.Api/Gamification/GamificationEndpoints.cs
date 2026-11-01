using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification;

public sealed record UpdateGamificationPreferencesRequest(
    bool BadgesPublic,
    bool RankingEnabled);

public static class GamificationEndpoints
{
    public static RouteGroupBuilder MapGamificationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gamification")
            .WithTags("Gamification")
            .RequireAuthorization();

        group.MapGet("/dashboard", GetDashboard)
            .WithName("GetGamificationDashboard")
            .Produces<GamificationDashboard>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/preferences", UpdatePreferences)
            .WithName("UpdateGamificationPreferences")
            .RequireRateLimiting("social-write")
            .Produces<GamificationPreferencesData>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/statistics", GetStatistics)
            .WithName("GetGamificationStatistics")
            .Produces<GamificationStatistics>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/users/{accountId:guid}/badges", GetPublicBadges)
            .WithName("GetPublicGamificationBadges")
            .Produces<PublicBadgesData>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/friends/ranking", GetMonthlyRanking)
            .WithName("GetMonthlyFriendsRanking")
            .Produces<GamificationRankingPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }

    private static async Task<IResult> GetDashboard(
        HttpContext context,
        GamificationService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(context, out var accountId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await service.GetDashboardAsync(accountId, cancellationToken));
    }

    private static async Task<IResult> UpdatePreferences(
        HttpContext context,
        GamificationService service,
        UpdateGamificationPreferencesRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(context, out var accountId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await service.UpdatePreferencesAsync(
            accountId,
            request.BadgesPublic,
            request.RankingEnabled,
            cancellationToken));
    }

    private static async Task<IResult> GetStatistics(
        HttpContext context,
        GamificationService service,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(context, out var accountId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetStatisticsAsync(
                accountId,
                start,
                end,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Período inválido.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> GetPublicBadges(
        HttpContext context,
        GamificationService service,
        Guid accountId,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(context, out _))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetPublicBadgesAsync(accountId, cancellationToken));
        }
        catch (KeyNotFoundException)
        {
            return Results.NotFound();
        }
    }

    private static async Task<IResult> GetMonthlyRanking(
        HttpContext context,
        GamificationService service,
        int year,
        int month,
        string? cursor,
        int? limit,
        CancellationToken cancellationToken)
    {
        if (!TryGetAccountId(context, out var accountId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetMonthlyRankingAsync(
                accountId,
                year,
                month,
                cursor,
                limit ?? 20,
                cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Consulta de ranking inválida.",
                detail: exception.Message);
        }
    }

    private static bool TryGetAccountId(HttpContext context, out Guid accountId) =>
        Guid.TryParse(context.User.FindFirst("sub")?.Value, out accountId) &&
        accountId != Guid.Empty;
}
