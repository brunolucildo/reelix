namespace Reelix.Application.Authentication;

public sealed record AuthenticatedAccount(
    Guid Id,
    string? Email,
    DateTimeOffset CreatedAt);
