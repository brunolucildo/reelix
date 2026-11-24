using Reelix.Api.Authentication;
using Reelix.Api.Gamification.Contracts;
using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Handlers;

/// <summary>Handler of <c>GET /api/v1/gamification/users/{accountId}/badges</c>.</summary>
internal static class GetPublicBadgesHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        Guid accountId,
        GetPublicBadgesUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is null)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(new GetPublicBadgesQuery(accountId), cancellationToken);
        return result is null
            ? Results.NotFound()
            : Results.Ok(PublicBadgesResponse.From(result));
    }
}
