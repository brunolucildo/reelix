using System.Text.Json;
using Reelix.Application.Authentication;
using Reelix.Domain.Social;

namespace Reelix.Application.Social;

public sealed class SocialService(
    IUserAccountRepository accounts,
    ISocialRepository socialRepository)
{
    private const int ActivityPageSize = 20;
    private const int RelationshipPageSize = 20;

    public async Task<IReadOnlyList<SocialUser>> SearchUsersAsync(
        Guid supabaseUserId,
        string query,
        int limit,
        CancellationToken cancellationToken)
    {
        var normalizedQuery = query.Trim();
        if (normalizedQuery.Length is < 2 or > 50)
        {
            throw new ArgumentException("A busca deve conter entre 2 e 50 caracteres.", nameof(query));
        }

        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await socialRepository.SearchUsersAsync(
            account.Id,
            normalizedQuery,
            Math.Clamp(limit, 1, 25),
            cancellationToken);
    }

    public async Task<FriendRelationship> RequestFriendshipAsync(
        Guid supabaseUserId,
        Guid targetAccountId,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        return await socialRepository.GetOrCreateRequestAsync(
            account.Id,
            targetAccountId,
            cancellationToken);
    }

    public async Task<FriendRelationship?> RespondToRequestAsync(
        Guid supabaseUserId,
        Guid relationshipId,
        bool accept,
        CancellationToken cancellationToken)
    {
        var account = await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken);
        var relationship = await socialRepository.GetRelationshipAsync(relationshipId, cancellationToken);
        if (relationship is null)
        {
            return null;
        }

        if (!relationship.Includes(account.Id))
        {
            return null;
        }

        if (accept)
        {
            relationship.Accept(account.Id);
            await socialRepository.SaveRelationshipAsync(
                relationship,
                account.Id,
                notifyOtherParticipant: true,
                cancellationToken);
        }
        else
        {
            relationship.Decline(account.Id);
            await socialRepository.SaveRelationshipAsync(
                relationship,
                account.Id,
                notifyOtherParticipant: false,
                cancellationToken);
        }

        return relationship;
    }

    public async Task<FriendRelationship?> CancelRequestAsync(
        Guid supabaseUserId,
        Guid relationshipId,
        CancellationToken cancellationToken)
    {
        var relationship = await GetOwnedRelationshipAsync(
            supabaseUserId,
            relationshipId,
            cancellationToken);
        if (relationship is null)
        {
            return null;
        }

        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        relationship.Cancel(accountId);
        await socialRepository.SaveRelationshipAsync(
            relationship,
            accountId,
            notifyOtherParticipant: false,
            cancellationToken);
        return relationship;
    }

    public async Task<FriendRelationship?> RemoveFriendAsync(
        Guid supabaseUserId,
        Guid relationshipId,
        CancellationToken cancellationToken)
    {
        var relationship = await GetOwnedRelationshipAsync(
            supabaseUserId,
            relationshipId,
            cancellationToken);
        if (relationship is null)
        {
            return null;
        }

        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        relationship.Remove(accountId);
        await socialRepository.SaveRelationshipAsync(
            relationship,
            accountId,
            notifyOtherParticipant: false,
            cancellationToken);
        return relationship;
    }

    public async Task<SocialPage<FriendItem>> GetFriendsAsync(
        Guid supabaseUserId,
        string? encodedCursor,
        CancellationToken cancellationToken)
    {
        var cursor = DecodeCursor(encodedCursor);
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var friends = await socialRepository.GetFriendsPageAsync(
            accountId,
            cursor,
            RelationshipPageSize + 1,
            cancellationToken);
        var hasMore = friends.Count > RelationshipPageSize;
        var page = friends.Take(RelationshipPageSize).ToArray();
        return new SocialPage<FriendItem>(
            page,
            hasMore && page.Length > 0
                ? EncodeCursor(page[^1].Since, page[^1].RelationshipId)
                : null);
    }

    public async Task<SocialPage<FriendRequest>> GetRequestsAsync(
        Guid supabaseUserId,
        bool incoming,
        string? encodedCursor,
        CancellationToken cancellationToken)
    {
        var cursor = DecodeCursor(encodedCursor);
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var requests = await socialRepository.GetRequestsAsync(
            accountId,
            incoming,
            cursor,
            RelationshipPageSize + 1,
            cancellationToken);
        var hasMore = requests.Count > RelationshipPageSize;
        var page = requests.Take(RelationshipPageSize).ToArray();
        return new SocialPage<FriendRequest>(
            page,
            hasMore && page.Length > 0
                ? EncodeCursor(page[^1].CreatedAt, page[^1].RelationshipId)
                : null);
    }

    public async Task<FriendProfile?> GetFriendProfileAsync(
        Guid supabaseUserId,
        Guid friendAccountId,
        CancellationToken cancellationToken)
    {
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return await socialRepository.GetFriendProfileAsync(
            accountId,
            friendAccountId,
            cancellationToken);
    }

    public async Task<SocialActivityPage> GetActivityFeedAsync(
        Guid supabaseUserId,
        string? encodedCursor,
        CancellationToken cancellationToken)
    {
        var cursor = DecodeCursor(encodedCursor);
        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        var activities = await socialRepository.GetActivityPageAsync(
            accountId,
            cursor,
            ActivityPageSize + 1,
            cancellationToken);
        var hasMore = activities.Count > ActivityPageSize;
        var page = activities.Take(ActivityPageSize).ToArray();
        return new SocialActivityPage(
            page,
            hasMore && page.Length > 0 ? EncodeCursor(page[^1]) : null);
    }

    private async Task<FriendRelationship?> GetOwnedRelationshipAsync(
        Guid supabaseUserId,
        Guid relationshipId,
        CancellationToken cancellationToken)
    {
        var relationship = await socialRepository.GetRelationshipAsync(relationshipId, cancellationToken);
        if (relationship is null)
        {
            return null;
        }

        var accountId = await GetAccountIdAsync(supabaseUserId, cancellationToken);
        return relationship.Includes(accountId) ? relationship : null;
    }

    private async Task<Guid> GetAccountIdAsync(Guid supabaseUserId, CancellationToken cancellationToken) =>
        (await accounts.GetOrCreateAsync(supabaseUserId, cancellationToken)).Id;

    private static string EncodeCursor(SocialActivity activity)
        => EncodeCursor(activity.OccurredAt, activity.Id);

    private static string EncodeCursor(DateTimeOffset occurredAt, Guid id)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(
            new SocialCursor(occurredAt, id));
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static SocialCursor? DecodeCursor(string? encodedCursor)
    {
        if (string.IsNullOrWhiteSpace(encodedCursor))
        {
            return null;
        }

        if (encodedCursor.Length > 200)
        {
            throw new ArgumentException("O cursor do feed é inválido.", nameof(encodedCursor));
        }

        try
        {
            var value = encodedCursor.Replace('-', '+').Replace('_', '/');
            value = value.PadRight(value.Length + ((4 - value.Length % 4) % 4), '=');
            var cursor = JsonSerializer.Deserialize<SocialCursor>(Convert.FromBase64String(value));
            if (cursor is null || cursor.Id == Guid.Empty)
            {
                throw new ArgumentException("O cursor do feed é inválido.", nameof(encodedCursor));
            }

            return cursor;
        }
        catch (FormatException exception)
        {
            throw new ArgumentException("O cursor do feed é inválido.", nameof(encodedCursor), exception);
        }
        catch (JsonException exception)
        {
            throw new ArgumentException("O cursor do feed é inválido.", nameof(encodedCursor), exception);
        }
    }
}
