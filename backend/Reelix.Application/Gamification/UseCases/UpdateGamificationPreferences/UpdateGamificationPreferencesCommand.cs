namespace Reelix.Application.Gamification;

public sealed record UpdateGamificationPreferencesCommand(
    Guid SubjectId,
    bool BadgesPublic,
    bool RankingEnabled);
