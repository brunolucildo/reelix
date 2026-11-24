using Microsoft.AspNetCore.RateLimiting;
using Reelix.Api.Errors;
using Reelix.Api.Gamification.Contracts;
using Reelix.Api.Gamification.Handlers;

namespace Reelix.Api.Gamification;

public static class GamificationEndpoints
{
    public static RouteGroupBuilder MapGamificationEndpoints(
        this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/gamification")
            .WithTags("Gamification")
            .RequireAuthorization();

        group.MapGet("/dashboard", GetGamificationDashboardHandler.HandleAsync)
            .WithName("GetGamificationDashboard")
            .WithApplicationProblems()
            .Produces<GamificationDashboardResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/preferences", UpdateGamificationPreferencesHandler.HandleAsync)
            .WithName("UpdateGamificationPreferences")
            .WithApplicationProblems()
            .RequireRateLimiting("social-write")
            .Produces<GamificationPreferencesResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/statistics", GetGamificationStatisticsHandler.HandleAsync)
            .WithName("GetGamificationStatistics")
            .WithApplicationProblems(new ApplicationProblemOptions { ValidationTitle = "Período inválido." })
            .Produces<GamificationStatisticsResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/users/{accountId:guid}/badges", GetPublicBadgesHandler.HandleAsync)
            .WithName("GetPublicGamificationBadges")
            .WithApplicationProblems()
            .Produces<PublicBadgesResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/friends/ranking", GetMonthlyRankingHandler.HandleAsync)
            .WithName("GetMonthlyFriendsRanking")
            .WithApplicationProblems(
                new ApplicationProblemOptions { ValidationTitle = "Consulta de ranking inválida." })
            .Produces<MonthlyRankingResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return group;
    }
}
