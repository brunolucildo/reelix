using Reelix.Domain.Gamification;

namespace Reelix.Application.Gamification;

/// <summary>The achievement definitions that are in force.</summary>
public interface IAchievementCatalog
{
    /// <summary>The active definitions ordered by code.</summary>
    Task<IReadOnlyList<AchievementDefinition>> ListActiveAsync(CancellationToken cancellationToken);
}

/// <summary>The achievements an account holds. The only write path to <c>user_achievements</c>.</summary>
public interface IUserAchievements
{
    /// <summary>All achievements of the account; the entities are tracked so changes are saved.</summary>
    Task<IReadOnlyList<UserAchievement>> ListAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>Adds the new achievements and saves every change made to the tracked ones.</summary>
    Task SaveAsync(IReadOnlyCollection<UserAchievement> granted, CancellationToken cancellationToken);
}

/// <summary>
/// The transactional boundary of the achievement reconciliation: a serializable-snapshot
/// transaction that also locks the account row so reconciliations of one account run one at a
/// time. The use case never sees the database behind it.
/// </summary>
public interface IAchievementReconciliationTransaction
{
    /// <returns>The open scope, or null when the account does not exist.</returns>
    Task<IReconciliationScope?> BeginAsync(Guid accountId, CancellationToken cancellationToken);
}

/// <summary>An open reconciliation transaction; leaving it without committing rolls it back.</summary>
public interface IReconciliationScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>Reads the watching statistics of an account.</summary>
public interface IGamificationStatisticsReader
{
    /// <summary>The statistics of the whole life of the account (used inside a reconciliation).</summary>
    Task<GamificationStatistics> GetLifetimeAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>The statistics of a period, read in one consistent snapshot.</summary>
    Task<GamificationStatistics> GetPeriodAsync(
        Guid accountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);
}

/// <summary>The gamification preferences of an account (badge visibility and ranking opt-in).</summary>
public interface IGamificationPreferencesStore
{
    /// <summary>The saved preferences, or null when the account never saved any (nothing is written).</summary>
    Task<GamificationPreferencesData?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>The saved preferences, creating the default row when there is none.</summary>
    Task<GamificationPreferencesData> EnsureAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>Stores the new values, creating the row when there is none.</summary>
    Task<GamificationPreferencesData> SaveAsync(
        Guid accountId,
        bool badgesPublic,
        bool rankingEnabled,
        CancellationToken cancellationToken);
}

/// <summary>Reads the monthly movie ranking among the friends of a viewer.</summary>
public interface IMonthlyRankingReader
{
    /// <summary>
    /// The candidates of one page (ordered, one more than <paramref name="limit"/> so the caller
    /// can tell whether there is a next page) and the distribution of scores of the whole ranking.
    /// </summary>
    Task<MonthlyRankingData> GetAsync(
        Guid viewerAccountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        GamificationRankingCursor? cursor,
        int limit,
        CancellationToken cancellationToken);
}

/// <summary>Reads the badges an account has earned.</summary>
public interface IPublicBadgesReader
{
    /// <summary>The earned badges of active definitions, oldest first.</summary>
    Task<IReadOnlyList<PublicBadge>> ListEarnedAsync(Guid accountId, CancellationToken cancellationToken);
}
