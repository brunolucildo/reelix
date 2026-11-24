namespace Reelix.Domain.Notifications;

/// <summary>
/// Decides whether a pending delivery may still be sent: its notification and device must exist,
/// the device must be active and belong to the same account, and the account must still want the
/// category.
/// </summary>
public static class PushDeliveryEligibility
{
    /// <returns>The reason to cancel the delivery, or null when it can be sent.</returns>
    public static string? CancellationReason(
        UserNotification? notification,
        NotificationDevice? device,
        NotificationPreferences? preferences)
    {
        if (notification is null ||
            device is null ||
            !device.IsActive ||
            notification.AccountId != device.AccountId)
        {
            return NotificationErrorCodes.DeviceOrNotificationInactive;
        }

        return preferences is null || !preferences.IsEnabled(notification.Category)
            ? NotificationErrorCodes.PreferenceDisabled
            : null;
    }
}
