using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Contracts;

/// <summary>HTTP contract of <c>GET /api/v1/gamification/friends/ranking</c>.</summary>
public sealed record MonthlyRankingResponse(
    bool IsEnabled,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    int TotalParticipants,
    IReadOnlyList<RankingEntryResponse> Items,
    string? NextCursor)
{
    public static MonthlyRankingResponse From(GetMonthlyRankingResult result) =>
        new(
            result.IsEnabled,
            result.PeriodStart,
            result.PeriodEnd,
            result.TotalParticipants,
            result.Items.Select(RankingEntryResponse.From).ToArray(),
            result.NextCursor);
}

public sealed record RankingEntryResponse(
    int Position,
    Guid AccountId,
    string DisplayName,
    int MoviesWatched)
{
    public static RankingEntryResponse From(GamificationRankingEntry entry) =>
        new(entry.Position, entry.AccountId, entry.DisplayName, entry.MoviesWatched);
}
