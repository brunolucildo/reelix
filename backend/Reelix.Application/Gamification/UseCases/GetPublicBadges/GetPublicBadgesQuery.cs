namespace Reelix.Application.Gamification;

/// <param name="AccountId">The internal id of the account whose badges are read (from the route).</param>
public sealed record GetPublicBadgesQuery(Guid AccountId);
