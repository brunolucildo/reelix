namespace Reelix.Application.Notifications;

public sealed record DeactivateNotificationDeviceCommand(Guid SubjectId, Guid DeviceId);
