namespace Reelix.Application.Notifications;

public sealed record GetNotificationInboxResult(
    IReadOnlyList<NotificationInboxItem> Items,
    string? NextCursor);
