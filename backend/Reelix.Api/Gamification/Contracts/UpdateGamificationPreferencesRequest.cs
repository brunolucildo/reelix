namespace Reelix.Api.Gamification.Contracts;

/// <summary>HTTP body of <c>PUT /api/v1/gamification/preferences</c>.</summary>
public sealed record UpdateGamificationPreferencesRequest(
    bool BadgesPublic,
    bool RankingEnabled);
