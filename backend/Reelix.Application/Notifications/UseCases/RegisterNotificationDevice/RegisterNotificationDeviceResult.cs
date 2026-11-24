namespace Reelix.Application.Notifications;

public sealed record RegisterNotificationDeviceResult(
    Guid Id,
    DevicePlatform Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastRegisteredAt);
