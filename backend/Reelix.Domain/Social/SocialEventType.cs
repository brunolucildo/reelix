namespace Reelix.Domain.Social;

/// <summary>
/// The kinds of social event the outbox carries. The values are persisted text (and guarded by a
/// check constraint), so they never change.
/// </summary>
public static class SocialEventType
{
    public const string FriendRequested = "friend.requested";

    public const string FriendAccepted = "friend.accepted";

    public static bool IsKnown(string? eventType) =>
        eventType is FriendRequested or FriendAccepted;
}
