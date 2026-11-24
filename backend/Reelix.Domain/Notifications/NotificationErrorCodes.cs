namespace Reelix.Domain.Notifications;

/// <summary>
/// The error codes written to <c>last_error_code</c> of a delivery. They are persisted text, so
/// the values never change.
/// </summary>
public static class NotificationErrorCodes
{
    public const string PreferenceDisabled = "preference_disabled";

    public const string DeviceOrNotificationInactive = "device_or_notification_inactive";

    public const string AccountChanged = "account_changed";

    public const string DeviceRemoved = "device_removed";

    public const string PushRejected = "expo_push_rejected";

    public const string PushTimeout = "expo_timeout";

    public const string PushTransportError = "expo_transport_error";

    public const string ReceiptCheckPending = "receipt_check_pending";

    public const string ReceiptNotReady = "receipt_not_ready";
}
