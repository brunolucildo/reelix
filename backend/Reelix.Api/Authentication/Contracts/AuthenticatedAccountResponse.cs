using Reelix.Application.Authentication;

namespace Reelix.Api.Authentication.Contracts;

/// <summary>
/// HTTP contract of <c>GET /api/v1/auth/me</c>. It is the pilot of the DTO pattern: contracts live
/// in <c>Api/&lt;Module&gt;/Contracts</c> as <c>&lt;Flow&gt;Request</c>/<c>&lt;Flow&gt;Response</c> and are
/// built from the use case result by an explicit mapping, so changing the application result never
/// changes the public JSON by accident.
/// </summary>
public sealed record AuthenticatedAccountResponse(
    Guid Id,
    string? Email,
    DateTimeOffset CreatedAt)
{
    public static AuthenticatedAccountResponse From(AuthenticatedAccount account) =>
        new(account.Id, account.Email, account.CreatedAt);
}
