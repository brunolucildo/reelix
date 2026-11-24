namespace Reelix.Application.Gamification;

/// <param name="SubjectId">The Supabase user id (<c>sub</c>) of the viewer.</param>
/// <param name="Cursor">The opaque cursor of the previous page, if any.</param>
/// <param name="Limit">Entries per page (1 to 50).</param>
public sealed record GetMonthlyRankingQuery(
    Guid SubjectId,
    int Year,
    int Month,
    string? Cursor,
    int Limit);
