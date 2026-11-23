using Reelix.Domain.Social;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

public sealed class FriendRelationshipTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

    [Fact]
    public void Create_StoresCanonicalPairAndRequestDirection()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();

        var relationship = FriendRelationship.Create(requester, addressee, Now);

        Assert.Equal(Now, relationship.CreatedAt);
        Assert.Equal(Now, relationship.UpdatedAt);
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
        var relationship = FriendRelationship.Create(requester, addressee, Now);

        Assert.Throws<InvalidOperationException>(() => relationship.Accept(requester, Now));
        relationship.Accept(addressee, Now.AddMinutes(1));
        Assert.Equal(Now.AddMinutes(1), relationship.UpdatedAt);
        Assert.Equal(FriendRelationshipStatus.Accepted, relationship.Status);

        relationship.Remove(requester, Now.AddMinutes(2));

        Assert.Equal(FriendRelationshipStatus.Removed, relationship.Status);
        Assert.Throws<InvalidOperationException>(() => relationship.Remove(addressee, Now));
    }

    [Fact]
    public void DeclinedRelationship_CanBeRequestedAgainOnlyByAValidPair()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var relationship = FriendRelationship.Create(requester, addressee, Now);
        relationship.Decline(addressee, Now);

        relationship.StartRequest(addressee, requester, Now.AddMinutes(1));

        Assert.Equal(FriendRelationshipStatus.Pending, relationship.Status);
        Assert.Equal(addressee, relationship.RequesterAccountId);
        Assert.Equal(requester, relationship.AddresseeAccountId);
        Assert.Throws<ArgumentException>(() => relationship.StartRequest(requester, Guid.NewGuid(), Now));
    }

    [Fact]
    public void RequesterCanCancelButCannotAnswerOwnPendingRequest()
    {
        var requester = Guid.NewGuid();
        var addressee = Guid.NewGuid();
        var relationship = FriendRelationship.Create(requester, addressee, Now);

        Assert.Throws<InvalidOperationException>(() => relationship.Decline(requester, Now));
        relationship.Cancel(requester, Now);

        Assert.Equal(FriendRelationshipStatus.Cancelled, relationship.Status);
    }

    [Fact]
    public void Create_RejectsSelfFriendship()
    {
        var accountId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => FriendRelationship.Create(accountId, accountId, Now));
    }
}
