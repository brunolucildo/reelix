namespace Reelix.Application.Authentication;

/// <summary>
/// Resolves the account behind <c>GET /auth/me</c>. This is the documented provisioning point:
/// the account is created here on the first authenticated call.
/// </summary>
public sealed class GetAuthenticatedAccount(IUserAccountRepository userAccountRepository)
{
    public async Task<AuthenticatedAccount> ExecuteAsync(
        Guid supabaseUserId,
        string? email,
        CancellationToken cancellationToken)
    {
        var account = await userAccountRepository.EnsureAsync(
            supabaseUserId,
            cancellationToken);

        return new AuthenticatedAccount(account.Id, email, account.CreatedAt);
    }
}
