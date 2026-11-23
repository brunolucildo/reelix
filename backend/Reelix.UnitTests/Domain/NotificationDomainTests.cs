using Reelix.Domain.Notifications;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Domain;

public sealed class NotificationDomainTests
{
    private static readonly DateTimeOffset Now = FixedTimeProvider.DefaultNow;

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
            "https://example.com",
            Now));

        var notification = UserNotification.Create(
            accountId,
            NotificationCategory.Social,
            "social:source",
            "Título",
            "Mensagem",
            "/social",
            Now);

        Assert.Equal("/social", notification.DeepLink);
        Assert.Equal(Now, notification.CreatedAt);
        Assert.Null(notification.ReadAt);
        notification.MarkRead(Now.AddMinutes(1));
        var readAt = notification.ReadAt;
        notification.MarkRead(Now.AddMinutes(2));
        Assert.Equal(Now.AddMinutes(1), readAt);
        Assert.Equal(readAt, notification.ReadAt);
    }

    [Fact]
    public void Delivery_RequiresLeaseAndWaitsForExpoReceipt()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var leaseId = Guid.NewGuid();

        Assert.Equal(Now, delivery.NextAttemptAt);
        Assert.Throws<InvalidOperationException>(() =>
            delivery.RecordTicket(Guid.NewGuid(), "ticket", Now.AddMinutes(15), Now));

        delivery.Claim(leaseId, Now.AddMinutes(2), Now);
        delivery.RecordTicket(leaseId, "ticket", Now.AddMinutes(15), Now);

        Assert.Equal(NotificationDeliveryStatus.Ticketed, delivery.Status);
        Assert.Equal(Now, delivery.ExpoTicketCreatedAt);
        delivery.RetryReceipt(Now.AddMinutes(30), "receipt_pending");
        Assert.Equal(1, delivery.ReceiptCheckCount);
        Assert.Throws<InvalidOperationException>(() => delivery.Claim(
            Guid.NewGuid(),
            Now.AddMinutes(2),
            Now));

        delivery.CompleteReceipt(delivered: true, Now.AddMinutes(31));

        Assert.Equal(NotificationDeliveryStatus.Completed, delivery.Status);
        Assert.Equal(Now.AddMinutes(31), delivery.CompletedAt);
    }

    [Fact]
    public void Delivery_RejectsATicketWhoseReceiptCheckIsNotInTheFuture()
    {
        var delivery = NotificationDelivery.Create(Guid.NewGuid(), Guid.NewGuid(), Now);
        var leaseId = Guid.NewGuid();
        delivery.Claim(leaseId, Now.AddMinutes(2), Now);

        Assert.Throws<ArgumentException>(() =>
            delivery.RecordTicket(leaseId, "ticket", Now, Now));
    }

    [Fact]
    public void Device_RejectsTokensFromAnotherProvider()
    {
        Assert.Throws<ArgumentException>(() => NotificationDevice.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "not-a-token",
            NotificationDevicePlatform.Android,
            Guid.NewGuid(),
            Now));
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
            projectId,
            Now);

        Assert.Throws<InvalidOperationException>(() => device.RegisterFor(
            newAccountId,
            "ExpoPushToken[abc12345678901234567890]",
            NotificationDevicePlatform.Android,
            projectId,
            Now.AddMinutes(1)));

        device.Deactivate();
        device.RegisterFor(
            newAccountId,
            "ExpoPushToken[abc12345678901234567890]",
            NotificationDevicePlatform.Android,
            projectId,
            Now.AddMinutes(2));

        Assert.Equal(newAccountId, device.AccountId);
        Assert.True(device.IsActive);
        Assert.Equal(Now, device.CreatedAt);
        Assert.Equal(Now.AddMinutes(2), device.LastRegisteredAt);
    }
}
