namespace Reelix.Application.Gamification;

public sealed record GetGamificationStatisticsQuery(
    Guid SubjectId,
    DateTimeOffset Start,
    DateTimeOffset End);
