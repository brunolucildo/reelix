using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Contracts;

/// <summary>HTTP contract of <c>GET /api/v1/gamification/dashboard</c>.</summary>
public sealed record GamificationDashboardResponse(
    GamificationPreferencesResponse Preferences,
    GamificationStatisticsResponse LifetimeStatistics,
    IReadOnlyList<AchievementProgressResponse> Achievements)
{
    public static GamificationDashboardResponse From(GetGamificationDashboardResult result) =>
        new(
            GamificationPreferencesResponse.From(result.Preferences),
            GamificationStatisticsResponse.From(result.LifetimeStatistics),
            result.Achievements.Select(AchievementProgressResponse.From).ToArray());
}

public sealed record AchievementProgressResponse(
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
    DateTimeOffset? ReactivatedAt)
{
    public static AchievementProgressResponse From(AchievementProgress progress) =>
        new(
            progress.Code,
            progress.Title,
            progress.Description,
            progress.CurrentValue,
            progress.Threshold,
            progress.IsEarned,
            progress.IsMetadataIncomplete,
            progress.Status,
            progress.FirstEarnedAt,
            progress.LastRevokedAt,
            progress.ReactivatedAt);
}
