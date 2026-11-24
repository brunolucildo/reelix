namespace Reelix.Application.Notifications;

/// <param name="SubjectId">The Supabase user id (<c>sub</c>) of the caller.</param>
/// <param name="ExpoPushToken">The push token; the provider is named by the HTTP contract (ADR 0006).</param>
public sealed record RegisterNotificationDeviceCommand(
    Guid SubjectId,
    Guid InstallationId,
    string? ExpoPushToken,
    DevicePlatform Platform,
    Guid ProjectId);
