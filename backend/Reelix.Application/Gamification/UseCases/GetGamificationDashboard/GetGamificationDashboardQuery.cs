namespace Reelix.Application.Gamification;

/// <param name="SubjectId">The Supabase user id (<c>sub</c>) of the caller.</param>
public sealed record GetGamificationDashboardQuery(Guid SubjectId);
