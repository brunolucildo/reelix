using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Authentication;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Users;

public sealed class UserAccountRepository(ReelixDbContext dbContext, TimeProvider clock)
    : IUserAccountRepository
{
    public Task<UserAccount?> FindAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken) =>
        dbContext.UserAccounts
            .SingleOrDefaultAsync(
                account => account.SupabaseUserId == supabaseUserId,
                cancellationToken);

    public async Task<UserAccount> EnsureAsync(
        Guid supabaseUserId,
        CancellationToken cancellationToken)
    {
        var existing = await FindAsync(supabaseUserId, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var account = UserAccount.Create(supabaseUserId, clock.GetUtcNow());
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
