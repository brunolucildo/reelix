using Reelix.Api.Authentication.Contracts;
using Reelix.Application.Authentication;

namespace Reelix.Api.Authentication.Handlers;

/// <summary>
/// Handler of <c>GET /api/v1/auth/me</c>: reads the caller, runs exactly one use case and maps its
/// result to the response contract. A missing or invalid <c>sub</c> is answered with 401 exactly as
/// the other authenticated routes do.
/// </summary>
internal static class GetAuthenticatedAccountHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        GetAuthenticatedAccount useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var account = await useCase.ExecuteAsync(
            subjectId,
            currentUser.Email,
            cancellationToken);

        return Results.Ok(AuthenticatedAccountResponse.From(account));
    }
}
