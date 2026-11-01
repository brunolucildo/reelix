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

    private sealed class TestAccountRepository(UserAccount account) : IUserAccountRepository
    {
        public Task<UserAccount> GetOrCreateAsync(
            Guid supabaseUserId,
            CancellationToken cancellationToken) =>
            Task.FromResult(account);
    }

    private sealed class TestSocialRepository(FriendRelationship relationship) : ISocialRepository
    {
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
            Task.FromResult<FriendProfile?>(null);

        public Task<IReadOnlyList<SocialActivity>> GetActivityPageAsync(
            Guid accountId,
            SocialCursor? cursor,
            int limit,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<SocialActivity>>([]);
    }
}
