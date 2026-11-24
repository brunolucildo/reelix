using Reelix.Domain.Notifications;
using Reelix.Domain.Social;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

/// <summary>The social event to notification decision, with no database (spec 0014, R5).</summary>
public sealed class SocialEventNotificationPlannerTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;
    private static readonly Guid Requester = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid Recipient = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");

    private static NotificationPreferences SocialOn()
    {
        var preferences = NotificationPreferences.Create(Recipient);
        preferences.Update(true, false, false);
        return preferences;
    }

    private static FriendRelationship Relationship(
        Guid requester,
        Guid addressee,
        FriendRelationshipStatus status)
    {
        var relationship = FriendRelationship.Create(requester, addressee, Now);
        switch (status)
        {
            case FriendRelationshipStatus.Accepted:
                relationship.Accept(addressee, Now);
                break;
            case FriendRelationshipStatus.Declined:
                relationship.Decline(addressee, Now);
                break;
            case FriendRelationshipStatus.Cancelled:
                relationship.Cancel(requester, Now);
                break;
        }

        return relationship;
    }

    private static NotificationDevice Device(Guid accountId, bool active = true)
    {
        var device = NotificationDevice.Create(
            accountId,
            Guid.NewGuid(),
            $"ExponentPushToken[{Guid.NewGuid():N}]",
            NotificationDevicePlatform.Android,
            Guid.NewGuid(),
            Now);
        if (!active)
        {
            device.Deactivate();
        }

        return device;
    }

    private static SocialEvent Event(string type, Guid actor, Guid recipient) =>
        SocialEvent.Create(type, actor, recipient, Now);

    [Fact]
    public void ARequestedEventWithAPendingRelationship_CreatesTheNotificationAndOneDeliveryPerActiveDevice()
    {
        var socialEvent = Event(SocialEventType.FriendRequested, Requester, Recipient);
        var active = Device(Recipient);
        var plan = SocialEventNotificationPlanner.Plan(
            socialEvent,
            Relationship(Requester, Recipient, FriendRelationshipStatus.Pending),
            SocialOn(),
            [active, Device(Recipient, active: false), Device(Requester)],
            Now);

        Assert.Equal(SocialEventOutcome.Created, plan.Outcome);
        var notification = plan.Notification!;
        Assert.Equal(Recipient, notification.AccountId);
        Assert.Equal(NotificationCategory.Social, notification.Category);
        Assert.Equal("Nova solicitação de amizade", notification.Title);
        Assert.Equal("Você recebeu uma solicitação de amizade.", notification.Body);
        Assert.Equal("/social", notification.DeepLink);
        Assert.Equal($"social:{socialEvent.Id}", notification.SourceKey);
        Assert.Equal(Now, notification.CreatedAt);
        var delivery = Assert.Single(plan.Deliveries);
        Assert.Equal(notification.Id, delivery.NotificationId);
        Assert.Equal(active.Id, delivery.DeviceId);
        Assert.Equal(NotificationDeliveryStatus.Pending, delivery.Status);
        Assert.Equal(Now, delivery.NextAttemptAt);
    }

    [Fact]
    public void AnAcceptedEventWithAnAcceptedRelationship_UsesTheAcceptedText()
    {
        var plan = SocialEventNotificationPlanner.Plan(
            Event(SocialEventType.FriendAccepted, Recipient, Requester),
            Relationship(Requester, Recipient, FriendRelationshipStatus.Accepted),
            SocialOnFor(Requester),
            [Device(Requester)],
            Now);

        Assert.Equal(SocialEventOutcome.Created, plan.Outcome);
        Assert.Equal("Solicitação aceita", plan.Notification!.Title);
        Assert.Equal("Sua solicitação de amizade foi aceita.", plan.Notification.Body);
        Assert.Equal(Requester, plan.Notification.AccountId);
    }

    [Fact]
    public void AnEventWithoutDevices_StillCreatesTheNotification()
    {
        var plan = SocialEventNotificationPlanner.Plan(
            Event(SocialEventType.FriendRequested, Requester, Recipient),
            Relationship(Requester, Recipient, FriendRelationshipStatus.Pending),
            SocialOn(),
            [],
            Now);

        Assert.Equal(SocialEventOutcome.Created, plan.Outcome);
        Assert.NotNull(plan.Notification);
        Assert.Empty(plan.Deliveries);
    }

    [Fact]
    public void WithoutSavedPreferences_TheOutcomeIsDisabled()
    {
        var plan = SocialEventNotificationPlanner.Plan(
            Event(SocialEventType.FriendRequested, Requester, Recipient),
            Relationship(Requester, Recipient, FriendRelationshipStatus.Pending),
            null,
            [Device(Recipient)],
            Now);

        Assert.Equal(SocialEventOutcome.Disabled, plan.Outcome);
        Assert.Null(plan.Notification);
        Assert.Empty(plan.Deliveries);
    }

    [Fact]
    public void WithSocialNotificationsOff_TheOutcomeIsDisabled()
    {
        var preferences = NotificationPreferences.Create(Recipient);
        preferences.Update(false, true, true);

        var plan = SocialEventNotificationPlanner.Plan(
            Event(SocialEventType.FriendRequested, Requester, Recipient),
            Relationship(Requester, Recipient, FriendRelationshipStatus.Pending),
            preferences,
            [Device(Recipient)],
            Now);

        Assert.Equal(SocialEventOutcome.Disabled, plan.Outcome);
    }

    [Theory]
    [InlineData("no-relationship")]
    [InlineData("requested-declined")]
    [InlineData("requested-cancelled")]
    [InlineData("requested-reversed-roles")]
    [InlineData("requested-accepted")]
    [InlineData("accepted-still-pending")]
    [InlineData("accepted-without-relationship")]
    [InlineData("other-pair")]
    public void WhenTheRelationshipNoLongerMatches_TheOutcomeIsObsoleteEvenWithPreferencesAndDevices(
        string scenario)
    {
        var type = scenario.StartsWith("accepted", StringComparison.Ordinal)
            ? SocialEventType.FriendAccepted
            : SocialEventType.FriendRequested;
        var actor = type == SocialEventType.FriendAccepted ? Recipient : Requester;
        var recipient = type == SocialEventType.FriendAccepted ? Requester : Recipient;
        FriendRelationship? relationship = scenario switch
        {
            "requested-declined" => Relationship(Requester, Recipient, FriendRelationshipStatus.Declined),
            "requested-cancelled" => Relationship(Requester, Recipient, FriendRelationshipStatus.Cancelled),
            "requested-reversed-roles" => Relationship(Recipient, Requester, FriendRelationshipStatus.Pending),
            "requested-accepted" => Relationship(Requester, Recipient, FriendRelationshipStatus.Accepted),
            "accepted-still-pending" => Relationship(Requester, Recipient, FriendRelationshipStatus.Pending),
            "other-pair" => Relationship(Guid.NewGuid(), Guid.NewGuid(), FriendRelationshipStatus.Pending),
            _ => null,
        };

        var plan = SocialEventNotificationPlanner.Plan(
            Event(type, actor, recipient),
            relationship,
            SocialOnFor(recipient),
            [Device(recipient)],
            Now);

        Assert.Equal(SocialEventOutcome.Obsolete, plan.Outcome);
        Assert.Null(plan.Notification);
    }

    [Theory]
    [InlineData(SocialEventOutcome.Created, "created")]
    [InlineData(SocialEventOutcome.Disabled, "disabled")]
    [InlineData(SocialEventOutcome.Obsolete, "obsolete")]
    public void TheOutcomeIsPersistedWithTheSameTextAsBefore(SocialEventOutcome outcome, string text)
    {
        var eventId = Guid.NewGuid();

        var receipt = ProcessedNotificationEvent.Create(eventId, outcome, Now);

        Assert.Equal(text, outcome.ToPersistedText());
        Assert.Equal(text, receipt.Outcome);
        Assert.Equal(eventId, receipt.EventId);
    }

    [Fact]
    public void TheEventTypesKeepTheirPersistedText()
    {
        Assert.Equal("friend.requested", SocialEventType.FriendRequested);
        Assert.Equal("friend.accepted", SocialEventType.FriendAccepted);
        Assert.True(SocialEventType.IsKnown("friend.requested"));
        Assert.False(SocialEventType.IsKnown("friend.removed"));
        Assert.False(SocialEventType.IsKnown(null));
    }

    private static NotificationPreferences SocialOnFor(Guid accountId)
    {
        var preferences = NotificationPreferences.Create(accountId);
        preferences.Update(true, false, false);
        return preferences;
    }
}
