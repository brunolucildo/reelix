namespace Reelix.Application.Gamification;

/// <summary>
/// Mirror of the Domain status of an achievement, so that no Domain type reaches a use case
/// result or the HTTP contract. Names and values must match the Domain enum (a test guards it).
/// </summary>
public enum AchievementStatus
{
    Earned,
    Revoked,
}

/// <summary>The progress of one achievement of the person.</summary>
public sealed record AchievementProgress(
    string Code,
    string Title,
    string Description,
    int CurrentValue,
    int Threshold,
    bool IsEarned,
    bool IsMetadataIncomplete,
    AchievementStatus? Status,
    DateTimeOffset? FirstEarnedAt,
    DateTimeOffset? LastRevokedAt,
    DateTimeOffset? ReactivatedAt);

/// <summary>Watching statistics of one account, for a period or for its whole life.</summary>
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

    /// <summary>The statistics of an account that has nothing recorded (no row is read or written).</summary>
    public static GamificationStatistics ForAccountWithoutActivity(
        DateTimeOffset? periodStart,
        DateTimeOffset? periodEnd,
        DateTimeOffset calculatedAt) =>
        new(
            periodStart ?? DateTimeOffset.MinValue,
            periodEnd ?? DateTimeOffset.MaxValue,
            0,
            0,
            0,
            0,
            0,
            0,
            0,
            null,
            null,
            [],
            null,
            null,
            null,
            calculatedAt);
}

public sealed record GamificationPreferencesData(
    bool BadgesPublic,
    bool RankingEnabled,
    DateTimeOffset UpdatedAt);

public sealed record PublicBadge(
    string Code,
    string Title,
    string Description,
    DateTimeOffset FirstEarnedAt);

/// <summary>The position of the last ranking entry of a page, used to ask for the next one.</summary>
public sealed record GamificationRankingCursor(int MoviesWatched, Guid AccountId);

/// <summary>One account of the monthly ranking as the store sees it (before positions).</summary>
public sealed record RankingCandidate(Guid AccountId, string DisplayName, int MoviesWatched);

/// <summary>How many participants reached exactly <paramref name="MoviesWatched"/> movies.</summary>
public sealed record RankingScoreBucket(int MoviesWatched, int Participants);

/// <summary>What the ranking store reads for one viewer and month.</summary>
public sealed record MonthlyRankingData(
    bool IsEnabled,
    IReadOnlyList<RankingCandidate> Candidates,
    IReadOnlyList<RankingScoreBucket> ScoreDistribution)
{
    public static MonthlyRankingData Disabled { get; } = new(false, [], []);
}
