using Microsoft.EntityFrameworkCore;
using Reelix.Application.Errors;
using Reelix.Application.Gamification;
using Reelix.Domain.Gamification;

namespace Reelix.Infrastructure.Persistence.Gamification;

/// <summary>The write adapter of <c>gamification_preferences</c>.</summary>
public sealed class GamificationPreferencesStore(ReelixDbContext dbContext, TimeProvider clock)
    : IGamificationPreferencesStore
{
    public async Task<GamificationPreferencesData?> FindAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var preferences = await dbContext.GamificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        return preferences is null ? null : ToData(preferences);
    }

    public async Task<GamificationPreferencesData> EnsureAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO public.gamification_preferences
                (account_id, badges_public, ranking_enabled, updated_at)
            VALUES ({accountId}, TRUE, FALSE, {clock.GetUtcNow()})
            ON CONFLICT (account_id) DO NOTHING
            """,
            cancellationToken);
        return ToData(await dbContext.GamificationPreferences
            .SingleAsync(item => item.AccountId == accountId, cancellationToken));
    }

    public async Task<GamificationPreferencesData> SaveAsync(
        Guid accountId,
        bool badgesPublic,
        bool rankingEnabled,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var account = await dbContext.UserAccounts
            .FromSqlInterpolated(
                $"SELECT * FROM public.user_accounts WHERE id = {accountId} FOR UPDATE")
            .ToArrayAsync(cancellationToken);
        if (account.Length == 0)
        {
            throw new NotFoundError("Conta não encontrada.");
        }

        var preferences = await dbContext.GamificationPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (preferences is null)
        {
            preferences = GamificationPreferences.Create(accountId, clock.GetUtcNow());
            dbContext.GamificationPreferences.Add(preferences);
        }

        preferences.Update(badgesPublic, rankingEnabled, clock.GetUtcNow());
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToData(preferences);
    }

    private static GamificationPreferencesData ToData(GamificationPreferences preferences) =>
        new(preferences.BadgesPublic, preferences.RankingEnabled, preferences.UpdatedAt);
}
