using Reelix.Api.Authentication;
using Reelix.Api.Gamification.Contracts;
using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Handlers;

/// <summary>Handler of <c>GET /api/v1/gamification/statistics</c>.</summary>
internal static class GetGamificationStatisticsHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        DateTimeOffset start,
        DateTimeOffset end,
        GetGamificationStatisticsUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new GetGamificationStatisticsQuery(subjectId, start, end),
            cancellationToken);
        return Results.Ok(GamificationStatisticsResponse.From(result.Statistics));
    }
}
