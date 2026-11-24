using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Contracts;

/// <summary>HTTP contract of <c>PUT /api/v1/gamification/preferences</c> (and the preferences of the dashboard).</summary>
public sealed record GamificationPreferencesResponse(
    bool BadgesPublic,
    bool RankingEnabled,
    DateTimeOffset UpdatedAt)
{
    public static GamificationPreferencesResponse From(GamificationPreferencesData preferences) =>
        new(preferences.BadgesPublic, preferences.RankingEnabled, preferences.UpdatedAt);
}
