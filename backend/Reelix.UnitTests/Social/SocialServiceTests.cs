using Reelix.Application.Authentication;
using Reelix.Application.Social;
using Reelix.Domain.Social;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Social;

public sealed class SocialServiceTests
{
    [Fact]
    public async Task RespondToRequest_ReturnsNotFoundForAnAccountOutsideTheRelationship()
    {
        var caller = UserAccount.Create(Guid.NewGuid());
        var relationship = FriendRelationship.Create(Guid.NewGuid(), Guid.NewGuid());
        var repository = new TestSocialRepository(relationship);
        var service = new SocialService(new TestAccountRepository(caller), repository);

        var result = await service.RespondToRequestAsync(
            caller.SupabaseUserId,
            relationship.Id,
            accept: true,
            CancellationToken.None);

        Assert.Null(result);
        Assert.False(repository.SaveWasCalled);
        Assert.Equal(FriendRelationshipStatus.Pending, relationship.Status);
    }

    [Fact]
    public async Task RespondToRequest_PersistsAcceptanceEventOnlyForRecipientAction()
    {
        var requester = Guid.NewGuid();
        var recipient = UserAccount.Create(Guid.NewGuid());
        var relationship = FriendRelationship.Create(requester, recipient.Id);
        var repository = new TestSocialRepository(relationship);
        var service = new SocialService(new TestAccountRepository(recipient), repository);

        var result = await service.RespondToRequestAsync(
            recipient.SupabaseUserId,
            relationship.Id,
            accept: true,
            CancellationToken.None);

        Assert.Same(relationship, result);
        Assert.Equal(FriendRelationshipStatus.Accepted, relationship.Status);
        Assert.True(repository.SaveWasCalled);
        Assert.True(repository.NotifyOtherParticipant);
        Assert.Equal(recipient.Id, repository.ActorAccountId);
    }

    [Theory]
    [InlineData("Movie", "movie")]
    [InlineData("Series", "series")]
    [InlineData("movie", "movie")]
    [InlineData("series", "series")]
    public async Task GetFriendProfile_ReturnsFavoriteContentTypesInTheApiFormat(
        string storedContentType,
        string expectedContentType)
    {
        var caller = UserAccount.Create(Guid.NewGuid());
        var friendAccountId = Guid.NewGuid();
        var favorite = new SharedFavorite(Guid.NewGuid(), storedContentType, "tmdb", "123");
        var repository = new TestSocialRepository(FriendRelationship.Create(caller.Id, friendAccountId))
        {
            Profile = new FriendProfile(friendAccountId, "Amigo", null, [favorite])
            {
                RelationshipStatus = FriendRelationshipStatus.Accepted,
                FavoritesShared = true,
            },
        };
        var service = new SocialService(new TestAccountRepository(caller), repository);

        var result = await service.GetFriendProfileAsync(
            caller.SupabaseUserId,
            friendAccountId,
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(friendAccountId, result.AccountId);
        Assert.Null(result.Biography);
        Assert.Equal(favorite with { ContentType = expectedContentType }, Assert.Single(result.Favorites));
    }

    [Fact]
    public async Task GetFriendProfile_PreservesAnUnavailableProfile()
    {
        var caller = UserAccount.Create(Guid.NewGuid());
        var friendAccountId = Guid.NewGuid();
        var repository = new TestSocialRepository(FriendRelationship.Create(caller.Id, friendAccountId));
        var service = new SocialService(new TestAccountRepository(caller), repository);

        var result = await service.GetFriendProfileAsync(
            caller.SupabaseUserId,
            friendAccountId,
            CancellationToken.None);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData(FriendRelationshipStatus.Pending, true)]
    [InlineData(FriendRelationshipStatus.Declined, true)]
    [InlineData(FriendRelationshipStatus.Cancelled, true)]
    [InlineData(FriendRelationshipStatus.Removed, true)]
    [InlineData(FriendRelationshipStatus.Accepted, false)]
    public async Task GetFriendProfile_DoesNotExposeFavoritesWithoutFriendshipAndConsent(
        FriendRelationshipStatus? status,
        bool shared)
    {
        var caller = UserAccount.Create(Guid.NewGuid());
        var friendAccountId = Guid.NewGuid();
        var repository = new TestSocialRepository(FriendRelationship.Create(caller.Id, friendAccountId))
        {
            Profile = new FriendProfile(friendAccountId, "Amigo", null,
                [new SharedFavorite(Guid.NewGuid(), "Movie", "tmdb", "123")])
            {
                RelationshipStatus = status,
                FavoritesShared = shared,
                WatchHistoryShared = shared,
                RatingsShared = shared,
            },
        };
        var service = new SocialService(new TestAccountRepository(caller), repository);

        var result = await service.GetFriendProfileAsync(caller.SupabaseUserId, friendAccountId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Favorites);
        Assert.False(result.FavoritesShared);
        if (status != FriendRelationshipStatus.Accepted)
        {
            Assert.False(result.WatchHistoryShared);
            Assert.False(result.RatingsShared);
        }
    }

    [Fact]
    public async Task GetActivityFeed_UsesTheResolvedCallerAndRequestedAuthor()
    {
        var caller = UserAccount.Create(Guid.NewGuid());
        var friendAccountId = Guid.NewGuid();
        var repository = new TestSocialRepository(FriendRelationship.Create(caller.Id, friendAccountId));
        var service = new SocialService(new TestAccountRepository(caller), repository);

        var result = await service.GetActivityFeedAsync(caller.SupabaseUserId, null, CancellationToken.None, friendAccountId);

        Assert.Equal(caller.Id, repository.ActivityCallerId);
        Assert.Equal(friendAccountId, repository.ActivityAuthorId);
        Assert.Empty(result.Items);
        Assert.Null(result.NextCursor);
    }

    private sealed class TestAccountRepository(UserAccount account) : IUserAccountRepository
    {
        public Task<UserAccount> GetOrCreateAsync(
            Guid supabaseUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(account);
    }

    private sealed class TestSocialRepository(FriendRelationship relationship) : ISocialRepository
    {
        public FriendProfile? Profile { get; init; }
        public Guid ActivityCallerId { get; private set; }
        public Guid? ActivityAuthorId { get; private set; }

        public bool SaveWasCalled { get; private set; }

        public bool NotifyOtherParticipant { get; private set; }

        public Guid ActorAccountId { get; private set; }

        public Task<IReadOnlyList<SocialUser>> SearchUsersAsync(
            Guid accountId,
            string query,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SocialUser>>([]);

        public Task<FriendRelationship> GetOrCreateRequestAsync(
            Guid requesterAccountId,
            Guid addresseeAccountId,
            CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<FriendRelationship?> GetRelationshipAsync(
            Guid relationshipId,
            CancellationToken cancellationToken) =>
            Task.FromResult<FriendRelationship?>(relationship.Id == relationshipId ? relationship : null);

        public Task SaveRelationshipAsync(
            FriendRelationship updatedRelationship,
            Guid actorAccountId,
            bool notifyOtherParticipant,
            CancellationToken cancellationToken)
        {
            SaveWasCalled = true;
            ActorAccountId = actorAccountId;
            NotifyOtherParticipant = notifyOtherParticipant;
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<FriendRequest>> GetRequestsAsync(
            Guid accountId,
            bool incoming,
            SocialCursor? cursor,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FriendRequest>>([]);

        public Task<IReadOnlyList<FriendItem>> GetFriendsPageAsync(
            Guid accountId,
            SocialCursor? cursor,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<FriendItem>>([]);

        public Task<FriendProfile?> GetFriendProfileAsync(
            Guid accountId,
            Guid friendAccountId,
            CancellationToken cancellationToken) =>
            Task.FromResult(Profile);

        public Task<IReadOnlyList<SocialActivity>> GetActivityPageAsync(
            Guid accountId,
            SocialCursor? cursor,
            int limit,
            CancellationToken cancellationToken,
            Guid? actorAccountId = null)
        {
            ActivityCallerId = accountId;
            ActivityAuthorId = actorAccountId;
            return Task.FromResult<IReadOnlyList<SocialActivity>>([]);
        }
    }
}
