using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// The only place where the Batch 0 tests touch the worker entry points (spec 0014). The tests
/// call a worker cycle through here, so the move of the processors to use cases changes this
/// file and no assertion.
/// </summary>
internal static class WorkerHarness
{
    public static async Task<int> ProcessSocialEventsAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var result = await scope.ServiceProvider
            .GetRequiredService<ProcessSocialEventsUseCase>()
            .ExecuteAsync(new ProcessSocialEventsCommand(), cancellationToken);
        return result.Processed;
    }

    public static async Task<int> DeliverPushAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var result = await scope.ServiceProvider
            .GetRequiredService<DeliverPushNotificationsUseCase>()
            .ExecuteAsync(new DeliverPushNotificationsCommand(), cancellationToken);
        return result.Processed;
    }
}

/// <summary>Direct-to-database seeding of notification data for the Batch 0 tests.</summary>
internal static class NotificationsSeeder
{
    public static async Task<NotificationDevice> AddDeviceAsync(
        this PostgresCharacterizationFixture fixture,
        Guid accountId,
        bool active = true,
        string? token = null)
    {
        await using var dbContext = fixture.CreateDbContext();
        var device = NotificationDevice.Create(
            accountId,
            Guid.NewGuid(),
            token ?? NewToken(),
            NotificationDevicePlatform.Android,
            PostgresCharacterizationFixture.ExpoProjectId,
            fixture.Clock.GetUtcNow());
        if (!active)
        {
            device.Deactivate();
        }

        dbContext.NotificationDevices.Add(device);
        await dbContext.SaveChangesAsync();
        return device;
    }

    public static async Task SetPreferencesAsync(
        this PostgresCharacterizationFixture fixture,
        Guid accountId,
        bool social,
        bool newEpisode = false,
        bool premiere = false)
    {
        await using var dbContext = fixture.CreateDbContext();
        var preferences = NotificationPreferences.Create(accountId);
        preferences.Update(social, newEpisode, premiere);
        dbContext.NotificationPreferences.Add(preferences);
        await dbContext.SaveChangesAsync();
    }

    public static async Task<SocialEvent> AddSocialEventAsync(
        this PostgresCharacterizationFixture fixture,
        string eventType,
        Guid actorAccountId,
        Guid recipientAccountId)
    {
        await using var dbContext = fixture.CreateDbContext();
        var socialEvent = SocialEvent.Create(
            eventType,
            actorAccountId,
            recipientAccountId,
            fixture.Clock.GetUtcNow());
        dbContext.SocialEvents.Add(socialEvent);
        await dbContext.SaveChangesAsync();
        return socialEvent;
    }

    /// <summary>Creates a relationship from <paramref name="requester"/> to <paramref name="addressee"/>.</summary>
    public static async Task AddRelationshipAsync(
        this PostgresCharacterizationFixture fixture,
        Guid requester,
        Guid addressee,
        FriendRelationshipStatus status)
    {
        await using var dbContext = fixture.CreateDbContext();
        var now = fixture.Clock.GetUtcNow();
        var relationship = FriendRelationship.Create(requester, addressee, now);
        switch (status)
        {
            case FriendRelationshipStatus.Accepted:
                relationship.Accept(addressee, now);
                break;
            case FriendRelationshipStatus.Declined:
                relationship.Decline(addressee, now);
                break;
            case FriendRelationshipStatus.Cancelled:
                relationship.Cancel(requester, now);
                break;
        }

        dbContext.FriendRelationships.Add(relationship);
        await dbContext.SaveChangesAsync();
    }

    public static async Task<UserNotification> AddNotificationAsync(
        this PostgresCharacterizationFixture fixture,
        Guid accountId,
        NotificationCategory category = NotificationCategory.Social,
        DateTimeOffset? createdAt = null)
    {
        await using var dbContext = fixture.CreateDbContext();
        var notification = UserNotification.Create(
            accountId,
            category,
            Guid.NewGuid().ToString("N"),
            "Título",
            "Corpo da notificação.",
            "/social",
            createdAt ?? fixture.Clock.GetUtcNow());
        dbContext.UserNotifications.Add(notification);
        await dbContext.SaveChangesAsync();
        return notification;
    }

    public static async Task<NotificationDelivery> AddPendingDeliveryAsync(
        this PostgresCharacterizationFixture fixture,
        Guid accountId,
        Guid deviceId,
        NotificationCategory category = NotificationCategory.Social)
    {
        var notification = await fixture.AddNotificationAsync(accountId, category);
        await using var dbContext = fixture.CreateDbContext();
        var delivery = NotificationDelivery.Create(notification.Id, deviceId, fixture.Clock.GetUtcNow());
        dbContext.NotificationDeliveries.Add(delivery);
        await dbContext.SaveChangesAsync();
        return delivery;
    }

    public static async Task<NotificationDelivery> ReadDeliveryAsync(
        this PostgresCharacterizationFixture fixture,
        Guid deliveryId)
    {
        await using var dbContext = fixture.CreateDbContext();
        return await dbContext.NotificationDeliveries.SingleAsync(item => item.Id == deliveryId);
    }

    public static async Task<NotificationDevice> ReadDeviceAsync(
        this PostgresCharacterizationFixture fixture,
        Guid deviceId)
    {
        await using var dbContext = fixture.CreateDbContext();
        return await dbContext.NotificationDevices.SingleAsync(item => item.Id == deviceId);
    }

    public static string NewToken() => $"ExponentPushToken[{Guid.NewGuid():N}]";
}
