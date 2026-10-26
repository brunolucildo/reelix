using Reelix.Domain.Users;

namespace Reelix.Application.Authentication;

public interface IUserAccountRepository
{
    Task<UserAccount> GetOrCreateAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken);
}
