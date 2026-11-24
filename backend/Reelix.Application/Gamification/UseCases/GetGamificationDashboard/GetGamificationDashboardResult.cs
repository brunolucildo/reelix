namespace Reelix.Application.Gamification;

public sealed record GetGamificationDashboardResult(
    GamificationPreferencesData Preferences,
    GamificationStatistics LifetimeStatistics,
    IReadOnlyList<AchievementProgress> Achievements);
