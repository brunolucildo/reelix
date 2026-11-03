using Microsoft.EntityFrameworkCore;
using Npgsql;
using Reelix.Application.Social;
using Reelix.Domain.Ratings;
using Reelix.Domain.Social;
using Reelix.Domain.Tracking;

namespace Reelix.Infrastructure.Persistence.Social;

public sealed class SocialRepository(ReelixDbContext dbContext) : ISocialRepository
{
    public async Task<IReadOnlyList<SocialUser>> SearchUsersAsync(
        Guid accountId,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var escapedQuery = query.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_");
        var profiles = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile =>
                profile.AccountId != accountId &&
                (profile.ProfileIsPublic || profile.IsDiscoverable) &&
                profile.DisplayName != null &&
                EF.Functions.ILike(profile.DisplayName, $"{escapedQuery}%", "\\"))
            .OrderBy(profile => profile.DisplayName)
            .ThenBy(profile => profile.AccountId)
            .Take(limit)
            .Select(profile => new { profile.AccountId, profile.DisplayName })
            .ToArrayAsync(cancellationToken);

        if (profiles.Length == 0)
        {
            return [];
        }

        var resultAccountIds = profiles.Select(profile => profile.AccountId).ToArray();
        var accountRelations = await dbContext.FriendRelationships
            .AsNoTracking()
            .Where(relationship =>
                (relationship.FirstAccountId == accountId && resultAccountIds.Contains(relationship.SecondAccountId)) ||
                (relationship.SecondAccountId == accountId && resultAccountIds.Contains(relationship.FirstAccountId)))
            .ToDictionaryAsync(relationship => relationship.OtherAccount(accountId), cancellationToken);

        return profiles
            .Select(profile =>
            {
                accountRelations.TryGetValue(profile.AccountId, out var relationship);
                return new SocialUser(
                    profile.AccountId,
                    profile.DisplayName!,
                    relationship?.Status,
                    relationship?.Id,
                    relationship?.RequesterAccountId == accountId);
            })
            .ToArray();
    }

    public async Task<FriendRelationship> GetOrCreateRequestAsync(
        Guid requesterAccountId,
        Guid addresseeAccountId,
        CancellationToken cancellationToken)
    {
        var addresseeCanBeFound = await dbContext.UserProfiles.AnyAsync(
            profile =>
                profile.AccountId == addresseeAccountId &&
                (profile.ProfileIsPublic || profile.IsDiscoverable),
            cancellationToken);
        if (!addresseeCanBeFound)
        {
            throw new KeyNotFoundException("Este perfil não está disponível para descoberta.");
        }

        var (first, second) = Order(requesterAccountId, addresseeAccountId);
        var relationship = await dbContext.FriendRelationships.SingleOrDefaultAsync(
            item => item.FirstAccountId == first && item.SecondAccountId == second,
            cancellationToken);
        if (relationship is null)
        {
            relationship = FriendRelationship.Create(requesterAccountId, addresseeAccountId);
            dbContext.FriendRelationships.Add(relationship);
        }
        else
        {
            relationship.StartRequest(requesterAccountId, addresseeAccountId);
        }

        dbContext.SocialEvents.Add(SocialEvent.Create(
            "friend.requested",
            requesterAccountId,
            addresseeAccountId));
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
            })
        {
            throw new InvalidOperationException(
                "Já existe uma solicitação ou amizade entre essas contas.",
                exception);
        }

        return relationship;
    }

    public Task<FriendRelationship?> GetRelationshipAsync(
        Guid relationshipId,
        CancellationToken cancellationToken) =>
        dbContext.FriendRelationships.SingleOrDefaultAsync(
            relationship => relationship.Id == relationshipId,
            cancellationToken);

    public async Task SaveRelationshipAsync(
        FriendRelationship relationship,
        Guid actorAccountId,
        bool notifyOtherParticipant,
        CancellationToken cancellationToken)
    {
        dbContext.FriendRelationships.Update(relationship);
        if (notifyOtherParticipant)
        {
            dbContext.SocialEvents.Add(SocialEvent.Create(
                "friend.accepted",
                actorAccountId,
                relationship.OtherAccount(actorAccountId)));
        }

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new InvalidOperationException(
                "A relação foi alterada por outra solicitação. Atualize os dados e tente novamente.",
                exception);
        }
    }

    public async Task<IReadOnlyList<FriendItem>> GetFriendsPageAsync(
        Guid accountId,
        SocialCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var relationships = await dbContext.FriendRelationships
            .AsNoTracking()
            .Where(relationship =>
                relationship.Status == FriendRelationshipStatus.Accepted &&
                (relationship.FirstAccountId == accountId || relationship.SecondAccountId == accountId) &&
                (cursor == null ||
                 relationship.UpdatedAt < cursor.OccurredAt ||
                 (relationship.UpdatedAt == cursor.OccurredAt &&
                  relationship.Id.CompareTo(cursor.Id) < 0)))
            .OrderByDescending(relationship => relationship.UpdatedAt)
            .ThenByDescending(relationship => relationship.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        var friendIds = relationships.Select(relationship => relationship.OtherAccount(accountId)).ToArray();
        var names = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => friendIds.Contains(profile.AccountId))
            .ToDictionaryAsync(
                profile => profile.AccountId,
                profile => profile.DisplayName,
                cancellationToken);

        return relationships
            .Select(relationship =>
            {
                var friendId = relationship.OtherAccount(accountId);
                names.TryGetValue(friendId, out var name);
                return new FriendItem(
                    relationship.Id,
                    friendId,
                    string.IsNullOrWhiteSpace(name)
                        ? "Usuário Reelix"
                        : name,
                    relationship.UpdatedAt);
            })
            .ToArray();
    }

    public async Task<IReadOnlyList<FriendRequest>> GetRequestsAsync(
        Guid accountId,
        bool incoming,
        SocialCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var requests = await dbContext.FriendRelationships
            .AsNoTracking()
            .Where(relationship =>
                relationship.Status == FriendRelationshipStatus.Pending &&
                (incoming
                    ? relationship.AddresseeAccountId == accountId
                    : relationship.RequesterAccountId == accountId) &&
                (cursor == null ||
                 relationship.CreatedAt < cursor.OccurredAt ||
                 (relationship.CreatedAt == cursor.OccurredAt &&
                  relationship.Id.CompareTo(cursor.Id) < 0)))
            .OrderByDescending(relationship => relationship.CreatedAt)
            .ThenByDescending(relationship => relationship.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        var userIds = requests
            .Select(request => incoming ? request.RequesterAccountId : request.AddresseeAccountId)
            .ToArray();
        var names = await dbContext.UserProfiles
            .AsNoTracking()
            .Where(profile => userIds.Contains(profile.AccountId))
            .ToDictionaryAsync(
                profile => profile.AccountId,
                profile => profile.DisplayName,
                cancellationToken);

        return requests
            .Select(request =>
            {
                var userId = incoming ? request.RequesterAccountId : request.AddresseeAccountId;
                names.TryGetValue(userId, out var name);
                return new FriendRequest(
                    request.Id,
                    userId,
                    string.IsNullOrWhiteSpace(name)
                        ? "Usuário Reelix"
                        : name,
                    request.CreatedAt);
            })
            .ToArray();
    }

    public async Task<FriendProfile?> GetFriendProfileAsync(
        Guid accountId,
        Guid friendAccountId,
        CancellationToken cancellationToken)
    {
        var relationship = await dbContext.FriendRelationships
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate =>
                (candidate.FirstAccountId == accountId && candidate.SecondAccountId == friendAccountId) ||
                (candidate.SecondAccountId == accountId && candidate.FirstAccountId == friendAccountId),
                cancellationToken);
        var profile = await dbContext.UserProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                candidate =>
                    candidate.AccountId == friendAccountId &&
                    (candidate.ProfileIsPublic || candidate.IsDiscoverable || candidate.AccountId == accountId ||
                     dbContext.FriendRelationships.Any(relation =>
                        (relation.Status == FriendRelationshipStatus.Accepted || relation.Status == FriendRelationshipStatus.Pending) &&
                        ((relation.FirstAccountId == accountId && relation.SecondAccountId == friendAccountId) ||
                         (relation.SecondAccountId == accountId && relation.FirstAccountId == friendAccountId)))),
                cancellationToken);
        if (profile is null)
        {
            return null;
        }

        var areFriends = relationship?.Status == FriendRelationshipStatus.Accepted;
        var favorites = areFriends && profile.FavoritesSharedWithFriends
            ? await dbContext.UserFavoriteTitles
                .AsNoTracking()
                .Join(
                    dbContext.ExternalCatalogReferences.AsNoTracking(),
                    favorite => favorite.TitleId,
                    reference => reference.Id,
                    (favorite, reference) => new { favorite, reference })
                .Where(item =>
                    item.favorite.AccountId == friendAccountId &&
                    dbContext.FriendRelationships.Any(relationship =>
                        relationship.Status == FriendRelationshipStatus.Accepted &&
                        ((relationship.FirstAccountId == accountId &&
                          relationship.SecondAccountId == friendAccountId) ||
                         (relationship.SecondAccountId == accountId &&
                          relationship.FirstAccountId == friendAccountId))))
                .OrderBy(item => item.favorite.ContentType)
                .ThenBy(item => item.favorite.TitleId)
                .Take(100)
                .Select(item => new SharedFavorite(
                    item.favorite.TitleId,
                    item.favorite.ContentType,
                    item.reference.Provider,
                    item.reference.ExternalId))
                .ToArrayAsync(cancellationToken)
            : [];

        return new FriendProfile(
            profile.AccountId,
            string.IsNullOrWhiteSpace(profile.DisplayName) ? "Usuário Reelix" : profile.DisplayName,
            profile.ProfileIsPublic ? profile.Biography : null,
            favorites)
        {
            RelationshipStatus = relationship?.Status,
            RelationshipId = relationship?.Id,
            RequestSentByMe = relationship?.RequesterAccountId == accountId,
            IsSelf = accountId == friendAccountId,
            FavoritesShared = areFriends && profile.FavoritesSharedWithFriends,
            WatchHistoryShared = areFriends && profile.WatchHistorySharedWithFriends,
            RatingsShared = areFriends && profile.RatingsSharedWithFriends,
            AvatarKey = profile.AvatarKey,
        };
    }

    public async Task<IReadOnlyList<SocialActivity>> GetActivityPageAsync(
        Guid accountId,
        SocialCursor? cursor,
        int limit,
        CancellationToken cancellationToken,
        Guid? actorAccountId = null)
    {
        var friendIds = dbContext.FriendRelationships
            .AsNoTracking()
            .Where(relationship =>
                relationship.Status == FriendRelationshipStatus.Accepted &&
                (relationship.FirstAccountId == accountId || relationship.SecondAccountId == accountId))
            .Select(relationship =>
                relationship.FirstAccountId == accountId
                    ? relationship.SecondAccountId
                    : relationship.FirstAccountId)
            .Where(friendId => !actorAccountId.HasValue || friendId == actorAccountId.Value);

        var trackedActivities = dbContext.TrackingRecords
            .AsNoTracking()
            .Join(
                dbContext.UserProfiles.AsNoTracking(),
                tracking => tracking.AccountId,
                profile => profile.AccountId,
                (tracking, profile) => new { tracking, profile })
            .Where(item =>
                friendIds.Contains(item.tracking.AccountId) &&
                item.profile.WatchHistorySharedWithFriends &&
                item.profile.WatchHistorySharedWithFriendsSince.HasValue &&
                item.tracking.CreatedAt >= item.profile.WatchHistorySharedWithFriendsSince.Value &&
                (cursor == null ||
                    item.tracking.CreatedAt < cursor.OccurredAt ||
                    (item.tracking.CreatedAt == cursor.OccurredAt && item.tracking.Id.CompareTo(cursor.Id) < 0)))
            .Select(item => new
            {
                Id = item.tracking.Id,
                ActorAccountId = item.tracking.AccountId,
                ActorDisplayName = item.profile.DisplayName,
                ActorAvatarKey = item.profile.AvatarKey,
                ActivityType = item.tracking.ItemType == TrackingItemType.Movie
                    ? "movieWatched"
                    : "episodeWatched",
                TargetId = item.tracking.TargetId,
                TargetType = item.tracking.ItemType == TrackingItemType.Movie ? "movie" : "episode",
                OccurredAt = item.tracking.CreatedAt,
            });

        var ratedActivities = dbContext.UserRatings
            .AsNoTracking()
            .Join(
                dbContext.UserProfiles.AsNoTracking(),
                rating => rating.AccountId,
                profile => profile.AccountId,
                (rating, profile) => new { rating, profile })
            .Where(item =>
                friendIds.Contains(item.rating.AccountId) &&
                item.rating.Audience == ReviewAudience.Public &&
                item.profile.RatingsSharedWithFriends &&
                item.profile.RatingsSharedWithFriendsSince.HasValue &&
                item.rating.UpdatedAt >= item.profile.RatingsSharedWithFriendsSince.Value &&
                (cursor == null ||
                    item.rating.UpdatedAt < cursor.OccurredAt ||
                    (item.rating.UpdatedAt == cursor.OccurredAt && item.rating.Id.CompareTo(cursor.Id) < 0)))
            .Select(item => new
            {
                Id = item.rating.Id,
                ActorAccountId = item.rating.AccountId,
                ActorDisplayName = item.profile.DisplayName,
                ActorAvatarKey = item.profile.AvatarKey,
                ActivityType = "ratingUpdated",
                TargetId = item.rating.TargetId,
                TargetType = item.rating.TargetType == ReviewTargetType.Movie
                    ? "movie"
                    : item.rating.TargetType == ReviewTargetType.Series
                        ? "series"
                        : "episode",
                OccurredAt = item.rating.UpdatedAt,
            });

        var activities = await trackedActivities
            .Concat(ratedActivities)
            .OrderByDescending(activity => activity.OccurredAt)
            .ThenByDescending(activity => activity.Id)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
        var targetIds = activities.Select(activity => activity.TargetId).Distinct().ToArray();
        var catalogReferences = await dbContext.ExternalCatalogReferences
            .AsNoTracking()
            .Where(reference => targetIds.Contains(reference.Id))
            .ToDictionaryAsync(
                reference => reference.Id,
                reference => new { reference.Provider, reference.ExternalId },
                cancellationToken);

        return activities
            .Select(activity =>
            {
                var catalogReference = catalogReferences[activity.TargetId];
                return new SocialActivity(
                    activity.Id,
                    activity.ActorAccountId,
                    string.IsNullOrWhiteSpace(activity.ActorDisplayName)
                        ? "Usuário Reelix"
                        : activity.ActorDisplayName,
                    activity.ActivityType,
                    activity.TargetId,
                    activity.TargetType,
                    catalogReference.Provider,
                    catalogReference.ExternalId,
                    activity.OccurredAt)
                {
                    ActorAvatarKey = activity.ActorAvatarKey,
                };
            })
            .ToArray();
    }

    private static (Guid First, Guid Second) Order(Guid first, Guid second) =>
        first.CompareTo(second) < 0 ? (first, second) : (second, first);

}
