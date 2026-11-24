using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// Batch 0 of spec 0014 (R2): the social event to notification decision, on PostgreSQL, driven
/// through the worker entry point.
/// </summary>
[Collection(PostgresCharacterizationCollection.Name)]
public sealed class SocialEventsCharacterizationTests(PostgresCharacterizationFixture fixture)
    : CharacterizationTestBase(fixture)
{
    private IServiceProvider Services => Fixture.PushEnabled.Services;

    [Fact]
    public async Task FriendRequested_CreatesOneNotificationAndOneDeliveryPerActiveDevice()
    {
        var requester = await Fixture.CreateAccountAsync();
        var recipient = await Fixture.CreateAccountAsync();
        await Fixture.AddRelationshipAsync(requester.AccountId, recipient.AccountId, FriendRelationshipStatus.Pending);
        await Fixture.SetPreferencesAsync(recipient.AccountId, social: true);
        var firstDevice = await Fixture.AddDeviceAsync(recipient.AccountId);
        var secondDevice = await Fixture.AddDeviceAsync(recipient.AccountId);
        await Fixture.AddDeviceAsync(recipient.AccountId, active: false);
        var socialEvent = await Fixture.AddSocialEventAsync(
            "friend.requested", requester.AccountId, recipient.AccountId);

        var processed = await WorkerHarness.ProcessSocialEventsAsync(Services);

        Assert.Equal(1, processed);
        await using var dbContext = Fixture.CreateDbContext();
        var notification = await dbContext.UserNotifications.SingleAsync(
            item => item.AccountId == recipient.AccountId);
        Assert.Equal(NotificationCategory.Social, notification.Category);
        Assert.Equal("Nova solicitação de amizade", notification.Title);
        Assert.Equal("Você recebeu uma solicitação de amizade.", notification.Body);
        Assert.Equal("/social", notification.DeepLink);
        Assert.Equal($"social:{socialEvent.Id}", notification.SourceKey);
        Assert.Equal(Clock.GetUtcNow(), notification.CreatedAt);
        var deliveries = await dbContext.NotificationDeliveries
            .Where(item => item.NotificationId == notification.Id)
            .ToArrayAsync();
        Assert.Equal(
            new[] { firstDevice.Id, secondDevice.Id }.Order(),
            deliveries.Select(item => item.DeviceId).Order());
        Assert.All(deliveries, delivery =>
        {
            Assert.Equal(NotificationDeliveryStatus.Pending, delivery.Status);
            Assert.Equal(0, delivery.AttemptCount);
            Assert.Equal(Clock.GetUtcNow(), delivery.NextAttemptAt);
        });
        var receipt = await dbContext.ProcessedNotificationEvents.SingleAsync(
            item => item.EventId == socialEvent.Id);
        Assert.Equal("created", receipt.Outcome);
        Assert.Equal(Clock.GetUtcNow(), receipt.ProcessedAt);
    }

    [Fact]
    public async Task ProcessingTheSameEventAgain_DoesNotDuplicateAnything()
    {
        var requester = await Fixture.CreateAccountAsync();
        var recipient = await Fixture.CreateAccountAsync();
        await Fixture.AddRelationshipAsync(requester.AccountId, recipient.AccountId, FriendRelationshipStatus.Pending);
        await Fixture.SetPreferencesAsync(recipient.AccountId, social: true);
        await Fixture.AddDeviceAsync(recipient.AccountId);
        await Fixture.AddSocialEventAsync("friend.requested", requester.AccountId, recipient.AccountId);

        Assert.Equal(1, await WorkerHarness.ProcessSocialEventsAsync(Services));
        Assert.Equal(0, await WorkerHarness.ProcessSocialEventsAsync(Services));

        await using var dbContext = Fixture.CreateDbContext();
        Assert.Equal(1, await dbContext.UserNotifications.CountAsync(item => item.AccountId == recipient.AccountId));
        Assert.Equal(
            1,
            await dbContext.NotificationDeliveries.CountAsync(
                item => dbContext.UserNotifications.Any(
                    notification => notification.Id == item.NotificationId &&
                        notification.AccountId == recipient.AccountId)));
    }

    [Fact]
    public async Task FriendAccepted_NotifiesTheRequesterWithTheAcceptedText()
    {
        var requester = await Fixture.CreateAccountAsync();
        var accepter = await Fixture.CreateAccountAsync();
        await Fixture.AddRelationshipAsync(requester.AccountId, accepter.AccountId, FriendRelationshipStatus.Accepted);
        await Fixture.SetPreferencesAsync(requester.AccountId, social: true);
        await Fixture.AddDeviceAsync(requester.AccountId);
        var socialEvent = await Fixture.AddSocialEventAsync(
            "friend.accepted", accepter.AccountId, requester.AccountId);

        Assert.Equal(1, await WorkerHarness.ProcessSocialEventsAsync(Services));

        await using var dbContext = Fixture.CreateDbContext();
        var notification = await dbContext.UserNotifications.SingleAsync(
            item => item.AccountId == requester.AccountId);
        Assert.Equal("Solicitação aceita", notification.Title);
        Assert.Equal("Sua solicitação de amizade foi aceita.", notification.Body);
        Assert.Equal("/social", notification.DeepLink);
        Assert.Equal($"social:{socialEvent.Id}", notification.SourceKey);
        Assert.Equal(
            "created",
            (await dbContext.ProcessedNotificationEvents.SingleAsync(item => item.EventId == socialEvent.Id)).Outcome);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WhenTheRecipientHasNotEnabledSocialNotifications_TheOutcomeIsDisabled(bool hasPreferenceRow)
    {
        var requester = await Fixture.CreateAccountAsync();
        var recipient = await Fixture.CreateAccountAsync();
        await Fixture.AddRelationshipAsync(requester.AccountId, recipient.AccountId, FriendRelationshipStatus.Pending);
        if (hasPreferenceRow)
        {
            await Fixture.SetPreferencesAsync(recipient.AccountId, social: false, newEpisode: true, premiere: true);
        }

        await Fixture.AddDeviceAsync(recipient.AccountId);
        var socialEvent = await Fixture.AddSocialEventAsync(
            "friend.requested", requester.AccountId, recipient.AccountId);

        Assert.Equal(1, await WorkerHarness.ProcessSocialEventsAsync(Services));

        await using var dbContext = Fixture.CreateDbContext();
        Assert.Equal(0, await dbContext.UserNotifications.CountAsync(item => item.AccountId == recipient.AccountId));
        Assert.Equal(
            "disabled",
            (await dbContext.ProcessedNotificationEvents.SingleAsync(item => item.EventId == socialEvent.Id)).Outcome);
    }

    [Theory]
    [InlineData("requested-then-declined")]
    [InlineData("requested-then-cancelled")]
    [InlineData("requested-with-reversed-roles")]
    [InlineData("requested-without-relationship")]
    [InlineData("accepted-while-still-pending")]
    public async Task WhenTheRelationshipNoLongerMatchesTheEvent_TheOutcomeIsObsolete(string scenario)
    {
        var actor = await Fixture.CreateAccountAsync();
        var recipient = await Fixture.CreateAccountAsync();
        var eventType = "friend.requested";
        switch (scenario)
        {
            case "requested-then-declined":
                await Fixture.AddRelationshipAsync(actor.AccountId, recipient.AccountId, FriendRelationshipStatus.Declined);
                break;
            case "requested-then-cancelled":
                await Fixture.AddRelationshipAsync(actor.AccountId, recipient.AccountId, FriendRelationshipStatus.Cancelled);
                break;
            case "requested-with-reversed-roles":
                await Fixture.AddRelationshipAsync(recipient.AccountId, actor.AccountId, FriendRelationshipStatus.Pending);
                break;
            case "accepted-while-still-pending":
                eventType = "friend.accepted";
                await Fixture.AddRelationshipAsync(recipient.AccountId, actor.AccountId, FriendRelationshipStatus.Pending);
                break;
        }

        await Fixture.SetPreferencesAsync(recipient.AccountId, social: true);
        await Fixture.AddDeviceAsync(recipient.AccountId);
        var socialEvent = await Fixture.AddSocialEventAsync(eventType, actor.AccountId, recipient.AccountId);

        Assert.Equal(1, await WorkerHarness.ProcessSocialEventsAsync(Services));

        await using var dbContext = Fixture.CreateDbContext();
        Assert.Equal(0, await dbContext.UserNotifications.CountAsync(item => item.AccountId == recipient.AccountId));
        Assert.Equal(
            "obsolete",
            (await dbContext.ProcessedNotificationEvents.SingleAsync(item => item.EventId == socialEvent.Id)).Outcome);
    }

    [Fact]
    public async Task TwoConcurrentBatches_NeverProcessTheSameEventTwice()
    {
        var requester = await Fixture.CreateAccountAsync();
        var recipient = await Fixture.CreateAccountAsync();
        await Fixture.AddRelationshipAsync(requester.AccountId, recipient.AccountId, FriendRelationshipStatus.Pending);
        await Fixture.SetPreferencesAsync(recipient.AccountId, social: true);
        await Fixture.AddDeviceAsync(recipient.AccountId);
        const int eventCount = 30;
        for (var index = 0; index < eventCount; index++)
        {
            await Fixture.AddSocialEventAsync("friend.requested", requester.AccountId, recipient.AccountId);
        }

        var batches = await Task.WhenAll(
            WorkerHarness.ProcessSocialEventsAsync(Services),
            WorkerHarness.ProcessSocialEventsAsync(Services));

        Assert.Equal(eventCount, batches.Sum());
        await using var dbContext = Fixture.CreateDbContext();
        Assert.Equal(eventCount, await dbContext.UserNotifications.CountAsync(item => item.AccountId == recipient.AccountId));
        Assert.Equal(
            eventCount,
            await dbContext.ProcessedNotificationEvents.CountAsync(
                item => dbContext.SocialEvents.Any(
                    socialEvent => socialEvent.Id == item.EventId &&
                        socialEvent.RecipientAccountId == recipient.AccountId)));
    }
}
