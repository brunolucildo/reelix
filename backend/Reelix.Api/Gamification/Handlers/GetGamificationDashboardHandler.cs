using Reelix.Api.Authentication;
using Reelix.Api.Gamification.Contracts;
using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Handlers;

/// <summary>Handler of <c>GET /api/v1/gamification/dashboard</c>.</summary>
internal static class GetGamificationDashboardHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        GetGamificationDashboardUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new GetGamificationDashboardQuery(subjectId),
            cancellationToken);
        return Results.Ok(GamificationDashboardResponse.From(result));
    }
}
