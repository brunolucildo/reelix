using Reelix.Domain.Gamification;

namespace Reelix.Application.Gamification;

public sealed record AchievementProgress(
    string Code,
    string Title,
    string Description,
    int CurrentValue,
    int Threshold,
    bool IsEarned,
    bool IsMetadataIncomplete,
    UserAchievementStatus? Status,
    DateTimeOffset? FirstEarnedAt,
    DateTimeOffset? LastRevokedAt,
    DateTimeOffset? ReactivatedAt);

public sealed record GamificationStatistics(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int MoviesWatched,
    int EpisodesWatched,
    int SeriesExplored,
    int GenresExplored,
    int MoviesMissingGenreMetadata,
    int ItemsWithRuntime,
    int ItemsWithoutRuntime,
    int? EstimatedWatchMinutes,
    decimal? EstimatedWatchHours,
    IReadOnlyList<string> RuntimeSources,
    int? RuntimeMetadataVersion,
    DateTimeOffset? RuntimeMetadataUpdatedAt,
    int? ConfirmedWatchMinutes,
    DateTimeOffset CalculatedAt)
{
    public double RuntimeCoverage =>
        ItemsWithRuntime + ItemsWithoutRuntime == 0
            ? 0
            : (double)ItemsWithRuntime / (ItemsWithRuntime + ItemsWithoutRuntime);
}

public sealed record GamificationPreferencesData(
    bool BadgesPublic,
    bool RankingEnabled,
    DateTimeOffset UpdatedAt);

public sealed record GamificationDashboard(
    GamificationPreferencesData Preferences,
    GamificationStatistics LifetimeStatistics,
    IReadOnlyList<AchievementProgress> Achievements);

public sealed record PublicBadge(
    string Code,
    string Title,
    string Description,
    DateTimeOffset FirstEarnedAt);

public sealed record PublicBadgesData(
    bool IsPublic,
    IReadOnlyList<PublicBadge> Badges);

public sealed record GamificationRankingEntry(
    int Position,
    Guid AccountId,
    string DisplayName,
    int MoviesWatched);

public sealed record GamificationRankingPage(
    bool IsEnabled,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int TotalParticipants,
    IReadOnlyList<GamificationRankingEntry> Items,
    string? NextCursor);

public sealed record GamificationRankingCursor(int MoviesWatched, Guid AccountId);

public interface IGamificationRepository
{
    Task<GamificationDashboard> GetDashboardAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<GamificationPreferencesData> UpdatePreferencesAsync(
        Guid accountId,
        bool badgesPublic,
        bool rankingEnabled,
        CancellationToken cancellationToken);

    Task<GamificationStatistics> GetStatisticsAsync(
        Guid accountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        CancellationToken cancellationToken);

    Task<PublicBadgesData> GetPublicBadgesAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<GamificationRankingPage> GetMonthlyRankingAsync(
        Guid accountId,
        DateTimeOffset periodStart,
        DateTimeOffset periodEnd,
        GamificationRankingCursor? cursor,
        int limit,
        CancellationToken cancellationToken);
}
