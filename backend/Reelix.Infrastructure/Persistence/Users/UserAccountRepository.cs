using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Authentication;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Users;

public sealed class UserAccountRepository(ReelixDbContext dbContext)
    : IUserAccountRepository
{
    public async Task<UserAccount> GetOrCreateAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.UserAccounts
            .SingleOrDefaultAsync(
                account => account.SupabaseUserId == supabaseUserId,
                cancellationToken);

        if (existing is not null)
        {
            return existing;
        }

        var account = UserAccount.Create(supabaseUserId);
        dbContext.UserAccounts.Add(account);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return account;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            })
        {
            dbContext.Entry(account).State = EntityState.Detached;
            return await dbContext.UserAccounts.SingleAsync(
                candidate => candidate.SupabaseUserId == supabaseUserId,
                cancellationToken);
        }
    }
}
