using Reelix.Api.Authentication;
using Reelix.Api.Gamification.Contracts;
using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Handlers;

/// <summary>Handler of <c>PUT /api/v1/gamification/preferences</c>.</summary>
internal static class UpdateGamificationPreferencesHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        UpdateGamificationPreferencesRequest request,
        UpdateGamificationPreferencesUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new UpdateGamificationPreferencesCommand(subjectId, request.BadgesPublic, request.RankingEnabled),
            cancellationToken);
        return Results.Ok(GamificationPreferencesResponse.From(result.Preferences));
    }
}
