namespace Reelix.Application.Gamification;

public sealed record GamificationRankingEntry(
    int Position,
    Guid AccountId,
    string DisplayName,
    int MoviesWatched);

public sealed record GetMonthlyRankingResult(
    bool IsEnabled,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int TotalParticipants,
    IReadOnlyList<GamificationRankingEntry> Items,
    string? NextCursor);
