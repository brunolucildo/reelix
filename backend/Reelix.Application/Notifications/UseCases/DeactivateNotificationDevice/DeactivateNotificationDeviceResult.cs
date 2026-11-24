namespace Reelix.Application.Notifications;

/// <param name="Deactivated">False when the device does not exist, is not the caller's or is already removed.</param>
public sealed record DeactivateNotificationDeviceResult(bool Deactivated);
