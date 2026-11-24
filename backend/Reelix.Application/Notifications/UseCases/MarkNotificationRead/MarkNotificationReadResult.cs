namespace Reelix.Application.Notifications;

/// <param name="Marked">False when the notification does not exist or is not the caller's.</param>
public sealed record MarkNotificationReadResult(bool Marked);
