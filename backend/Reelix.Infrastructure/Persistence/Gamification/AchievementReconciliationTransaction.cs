using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Reelix.Application.Gamification;

namespace Reelix.Infrastructure.Persistence.Gamification;

/// <summary>
/// Opens the repeatable-read transaction of a reconciliation and locks the account row, so two
/// reconciliations of the same account never run together.
/// </summary>
public sealed class AchievementReconciliationTransaction(ReelixDbContext dbContext)
    : IAchievementReconciliationTransaction
{
    public async Task<IReconciliationScope?> BeginAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.RepeatableRead,
            cancellationToken);
        try
        {
            var account = await dbContext.UserAccounts
                .FromSqlInterpolated(
                    $"SELECT * FROM public.user_accounts WHERE id = {accountId} FOR UPDATE")
                .ToArrayAsync(cancellationToken);
            if (account.Length == 0)
            {
                await transaction.DisposeAsync();
                return null;
            }

            return new Scope(transaction);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class Scope(IDbContextTransaction transaction) : IReconciliationScope
    {
        public Task CommitAsync(CancellationToken cancellationToken) =>
            transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
