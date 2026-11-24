namespace Reelix.Application.Gamification;

public sealed record GetPublicBadgesResult(bool IsPublic, IReadOnlyList<PublicBadge> Badges);
