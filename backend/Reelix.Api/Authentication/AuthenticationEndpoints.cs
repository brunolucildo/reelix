using Reelix.Api.Authentication.Handlers;
using Reelix.Api.Errors;

namespace Reelix.Api.Authentication;

public static class AuthenticationEndpoints
{
    public static RouteGroupBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/auth")
            .WithTags("Authentication")
            .RequireAuthorization()
            .WithApplicationProblems();

        group.MapGet("/me", GetAuthenticatedAccountHandler.HandleAsync)
            .WithName("GetAuthenticatedAccount");

        return group;
    }
}
