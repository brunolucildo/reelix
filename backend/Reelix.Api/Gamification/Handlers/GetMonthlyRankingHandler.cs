using Reelix.Api.Authentication;
using Reelix.Api.Gamification.Contracts;
using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Handlers;

/// <summary>Handler of <c>GET /api/v1/gamification/friends/ranking</c>.</summary>
internal static class GetMonthlyRankingHandler
{
    private const int DefaultLimit = 20;

    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        int year,
        int month,
        string? cursor,
        int? limit,
        GetMonthlyRankingUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new GetMonthlyRankingQuery(subjectId, year, month, cursor, limit ?? DefaultLimit),
            cancellationToken);
        return Results.Ok(MonthlyRankingResponse.From(result));
    }
}
