using Reelix.Domain.Social;

namespace Reelix.UnitTests.Domain;

public sealed class FriendRelationshipTests
{
    [Fact]
    public void Create_StoresCanonicalPairAndRequestDirection()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();

        var relationship = FriendRelationship.Create(requester, addressee);

        Assert.Equal(FriendRelationshipStatus.Pending, relationship.Status);
        Assert.Equal(requester, relationship.RequesterAccountId);
        Assert.Equal(addressee, relationship.AddresseeAccountId);
        Assert.True(relationship.FirstAccountId.CompareTo(relationship.SecondAccountId) < 0);
        Assert.Equal(addressee, relationship.OtherAccount(requester));
    }

    [Fact]
    public void Accept_RequiresPendingRecipientAndRemovePreservesTerminalRelationship()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var relationship = FriendRelationship.Create(requester, addressee);

        Assert.Throws<InvalidOperationException>(() => relationship.Accept(requester));
        relationship.Accept(addressee);
        Assert.Equal(FriendRelationshipStatus.Accepted, relationship.Status);

        relationship.Remove(requester);

        Assert.Equal(FriendRelationshipStatus.Removed, relationship.Status);
        Assert.Throws<InvalidOperationException>(() => relationship.Remove(addressee));
    }

    [Fact]
    public void DeclinedRelationship_CanBeRequestedAgainOnlyByAValidPair()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var relationship = FriendRelationship.Create(requester, addressee);
        relationship.Decline(addressee);

        relationship.StartRequest(addressee, requester);

        Assert.Equal(FriendRelationshipStatus.Pending, relationship.Status);
        Assert.Equal(addressee, relationship.RequesterAccountId);
        Assert.Equal(requester, relationship.AddresseeAccountId);
        Assert.Throws<ArgumentException>(() => relationship.StartRequest(requester, Guid.NewGuid()));
    }

    [Fact]
    public void RequesterCanCancelButCannotAnswerOwnPendingRequest()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var relationship = FriendRelationship.Create(requester, addressee);

        Assert.Throws<InvalidOperationException>(() => relationship.Decline(requester));
        relationship.Cancel(requester);

        Assert.Equal(FriendRelationshipStatus.Cancelled, relationship.Status);
    }

    [Fact]
    public void Create_RejectsSelfFriendship()
    {
        var accountId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => FriendRelationship.Create(accountId, accountId));
    }
}
