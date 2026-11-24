namespace Reelix.Application.Notifications;

public sealed record MarkNotificationReadCommand(Guid SubjectId, Guid NotificationId);
