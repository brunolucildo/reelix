using Reelix.Domain.Notifications;

namespace Reelix.UnitTests.Domain;

public sealed class NotificationDomainTests
{
    [Fact]
    public void Preferences_DefaultToDisabledAndUpdateByCategory()
    {
        var preferences = NotificationPreferences.Create(Guid.NewGuid());

        Assert.False(preferences.IsEnabled(NotificationCategory.Social));
        Assert.False(preferences.IsEnabled(NotificationCategory.NewEpisode));
        Assert.False(preferences.IsEnabled(NotificationCategory.Premiere));

        preferences.Update(socialEnabled: true, newEpisodeEnabled: false, premiereEnabled: true);

        Assert.True(preferences.IsEnabled(NotificationCategory.Social));
        Assert.False(preferences.IsEnabled(NotificationCategory.NewEpisode));
        Assert.True(preferences.IsEnabled(NotificationCategory.Premiere));
    }

    [Fact]
    public void Notification_RequiresInternalDeepLinkAndValidContent()
    {
        var accountId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => UserNotification.Create(
            accountId,
            NotificationCategory.Social,
            "social:source",
            "Título",
            "Mensagem",
            "https://example.com"));

        var notification = UserNotification.Create(
            accountId,
            NotificationCategory.Social,
            "social:source",
            "Título",
            "Mensagem",
            "/social");

        Assert.Equal("/social", notification.DeepLink);
        Assert.Null(notification.ReadAt);
        notification.MarkRead();
        var readAt = notification.ReadAt;
        notification.MarkRead();
        Assert.Equal(readAt, notification.ReadAt);
    }

    [Fact]
    public void Delivery_RequiresLeaseAndWaitsForExpoReceipt()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;
        var leaseId = Guid.NewGuid();

        Assert.Throws<InvalidOperationException>(() =>
            delivery.RecordTicket(Guid.NewGuid(), "ticket", now.AddMinutes(15)));

        delivery.Claim(leaseId, now.AddMinutes(2), now);
        delivery.RecordTicket(leaseId, "ticket", DateTimeOffset.UtcNow.AddMinutes(15));

        Assert.Equal(NotificationDeliveryStatus.Ticketed, delivery.Status);
        Assert.NotNull(delivery.ExpoTicketCreatedAt);
        delivery.RetryReceipt(DateTimeOffset.UtcNow.AddMinutes(30), "receipt_pending");
        Assert.Equal(1, delivery.ReceiptCheckCount);
        Assert.Throws<InvalidOperationException>(() => delivery.Claim(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(2),
            DateTimeOffset.UtcNow));

        delivery.CompleteReceipt(delivered: true);

        Assert.Equal(NotificationDeliveryStatus.Completed, delivery.Status);
    }

    [Fact]
    public void Device_RejectsTokensFromAnotherProvider()
    {
        Assert.Throws<ArgumentException>(() => NotificationDevice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "not-a-token",
            NotificationDevicePlatform.Android,
            Guid.NewGuid()));
    }

    [Fact]
    public void ActiveDevice_CannotBeTransferredUntilItIsDeactivated()
    {
        var oldAccountId = Guid.NewGuid();
        var newAccountId = Guid.NewGuid();
        var installationId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var device = NotificationDevice.Create(
            oldAccountId,
            installationId,
            "ExpoPushToken[abc12345678901234567890]",
            NotificationDevicePlatform.Android,
            projectId);

        Assert.Throws<InvalidOperationException>(() => device.RegisterFor(
            newAccountId,
            "ExpoPushToken[abc12345678901234567890]",
            NotificationDevicePlatform.Android,
            projectId));

        device.Deactivate();
        device.RegisterFor(
            newAccountId,
            "ExpoPushToken[abc12345678901234567890]",
            NotificationDevicePlatform.Android,
            projectId);

        Assert.Equal(newAccountId, device.AccountId);
        Assert.True(device.IsActive);
    }
}
