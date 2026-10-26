namespace Reelix.Application.Authentication;

public sealed class GetAuthenticatedAccount(IUserAccountRepository userAccountRepository)
{
    public async Task<AuthenticatedAccount> ExecuteAsync(
        Guid supabaseUserId,
        string? email,
        CancellationToken cancellationToken)
    {
        var account = await userAccountRepository.GetOrCreateAsync(
            supabaseUserId,
            cancellationToken);

        return new AuthenticatedAccount(account.Id, email, account.CreatedAt);
    }
}
