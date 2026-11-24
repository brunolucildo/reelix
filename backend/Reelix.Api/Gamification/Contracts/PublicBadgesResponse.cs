using Reelix.Application.Gamification;

namespace Reelix.Api.Gamification.Contracts;

/// <summary>HTTP contract of <c>GET /api/v1/gamification/users/{accountId}/badges</c>.</summary>
public sealed record PublicBadgesResponse(bool IsPublic, IReadOnlyList<PublicBadgeResponse> Badges)
{
    public static PublicBadgesResponse From(GetPublicBadgesResult result) =>
        new(result.IsPublic, result.Badges.Select(PublicBadgeResponse.From).ToArray());
}

public sealed record PublicBadgeResponse(
    string Code,
    string Title,
    string Description,
    DateTimeOffset FirstEarnedAt)
{
    public static PublicBadgeResponse From(PublicBadge badge) =>
        new(badge.Code, badge.Title, badge.Description, badge.FirstEarnedAt);
}
