using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Contracts;

/// <summary>
/// HTTP body of <c>POST /api/v1/notifications/devices</c>. The platform accepts the same inputs as
/// before: a name in any letter case or a number.
/// </summary>
public sealed record RegisterNotificationDeviceRequest(
    Guid InstallationId,
    string ExpoPushToken,
    DevicePlatform Platform,
    Guid ProjectId);

/// <summary>HTTP contract of <c>POST /api/v1/notifications/devices</c>.</summary>
public sealed record NotificationDeviceResponse(
    Guid Id,
    DevicePlatform Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastRegisteredAt)
{
    public static NotificationDeviceResponse From(RegisterNotificationDeviceResult result) =>
        new(result.Id, result.Platform, result.CreatedAt, result.LastRegisteredAt);
}
