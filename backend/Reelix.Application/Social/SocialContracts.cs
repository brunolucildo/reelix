using Reelix.Domain.Social;

namespace Reelix.Application.Social;

public sealed record SocialUser(
    Guid AccountId,
    string DisplayName,
    FriendRelationshipStatus? RelationshipStatus,
    Guid? RelationshipId,
    bool RequestSentByMe);

public sealed record FriendRequest(
    Guid RelationshipId,
    Guid AccountId,
    string DisplayName,
    DateTimeOffset CreatedAt);

public sealed record FriendItem(
    Guid RelationshipId,
    Guid AccountId,
    string DisplayName,
    DateTimeOffset Since);

public sealed record SocialActivity(
    Guid Id,
    Guid ActorAccountId,
    string ActorDisplayName,
    string ActivityType,
    Guid TargetId,
    string TargetType,
    string Provider,
    string ExternalId,
    DateTimeOffset OccurredAt)
{
    public string? ActorAvatarKey { get; init; }
}

public sealed record SocialActivityPage(
    IReadOnlyList<SocialActivity> Items,
    string? NextCursor);

public sealed record SocialPage<T>(
    IReadOnlyList<T> Items,
    string? NextCursor);

public sealed record FriendProfile(
    Guid AccountId,
    string DisplayName,
    string? Biography,
    IReadOnlyList<SharedFavorite> Favorites)
{
    public FriendRelationshipStatus? RelationshipStatus { get; init; }
    public Guid? RelationshipId { get; init; }
    public bool RequestSentByMe { get; init; }
    public bool IsSelf { get; init; }
    public bool FavoritesShared { get; init; }
    public bool WatchHistoryShared { get; init; }
    public bool RatingsShared { get; init; }
    public string? AvatarKey { get; init; }
}

public sealed record SharedFavorite(
    Guid TitleId,
    string ContentType,
    string Provider,
    string ExternalId);

public sealed record SocialCursor(DateTimeOffset OccurredAt, Guid Id);

public interface ISocialRepository
{
    Task<IReadOnlyList<SocialUser>> SearchUsersAsync(
        Guid accountId,
        string query,
        int limit,
        CancellationToken cancellationToken);

    Task<Reelix.Domain.Social.FriendRelationship> GetOrCreateRequestAsync(
        Guid requesterAccountId,
        Guid addresseeAccountId,
        CancellationToken cancellationToken);

    Task<Reelix.Domain.Social.FriendRelationship?> GetRelationshipAsync(
        Guid relationshipId,
        CancellationToken cancellationToken);

    Task SaveRelationshipAsync(
        Reelix.Domain.Social.FriendRelationship relationship,
        Guid actorAccountId,
        bool notifyOtherParticipant,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendRequest>> GetRequestsAsync(
        Guid accountId,
        bool incoming,
        SocialCursor? cursor,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<FriendItem>> GetFriendsPageAsync(
        Guid accountId,
        SocialCursor? cursor,
        int limit,
        CancellationToken cancellationToken);

    Task<FriendProfile?> GetFriendProfileAsync(
        Guid accountId,
        Guid friendAccountId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<SocialActivity>> GetActivityPageAsync(
        Guid accountId,
        SocialCursor? cursor,
        int limit,
        CancellationToken cancellationToken,
        Guid? actorAccountId = null);
}
