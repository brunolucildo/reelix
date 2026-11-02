using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

public sealed record UpdateNotificationPreferences(
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);

public sealed record RegisterNotificationDevice(
    Guid InstallationId,
    string ExpoPushToken,
    NotificationDevicePlatform Platform,
    Guid ProjectId);

public sealed record NotificationPreferencesData(
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);

public sealed record NotificationDeviceData(
    Guid Id,
    NotificationDevicePlatform Platform,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastRegisteredAt);

public sealed record NotificationInboxItem(
    Guid Id,
    NotificationCategory Category,
    string Title,
    string Body,
    string DeepLink,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

public sealed record NotificationInboxPage(
    IReadOnlyList<NotificationInboxItem> Items,
    string? NextCursor);

public sealed record NotificationUnreadCount(int Count);

public sealed record NotificationCursor(DateTimeOffset CreatedAt, Guid Id);

public sealed record NotificationPushWorkItem(
    Guid DeliveryId,
    Guid DeviceId,
    Guid LeaseId,
    string PushToken,
    Guid NotificationId,
    string Title,
    string Body,
    string DeepLink);

public sealed record NotificationReceiptWorkItem(
    Guid DeliveryId,
    Guid DeviceId,
    string TicketId);

public sealed record ExpoPushResponse(
    bool Accepted,
    string? TicketId,
    string? ErrorCode,
    TimeSpan? RetryAfter,
    bool DeviceNotRegistered,
    bool Retryable);

public sealed record ExpoReceiptResponse(
    bool Ready,
    bool Delivered,
    string? ErrorCode,
    bool DeviceNotRegistered);

public sealed class NotificationDeviceConflictException(string message) : Exception(message);

public interface IPushRegistrationConfiguration
{
    bool IsEnabled { get; }

    Guid? ProjectId { get; }

    int MaxAttempts { get; }
}

public interface IExpoPushClient
{
    Task<ExpoPushResponse> SendAsync(
        NotificationPushWorkItem workItem,
        CancellationToken cancellationToken);

    Task<ExpoReceiptResponse> GetReceiptAsync(
        string ticketId,
        CancellationToken cancellationToken);
}

public interface INotificationsRepository
{
    Task<NotificationPreferences> GetPreferencesAsync(
        Guid accountId,
        CancellationToken cancellationToken);

    Task<NotificationPreferences> UpdatePreferencesAsync(
        Guid accountId,
        UpdateNotificationPreferences preferences,
        CancellationToken cancellationToken);

    Task<NotificationDevice> RegisterDeviceAsync(
        Guid accountId,
        RegisterNotificationDevice registration,
        CancellationToken cancellationToken);

    Task<bool> DeactivateDeviceAsync(
        Guid accountId,
        Guid deviceId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationInboxItem>> GetInboxPageAsync(
        Guid accountId,
        NotificationCursor? cursor,
        int limit,
        CancellationToken cancellationToken);

    Task<int> GetUnreadCountAsync(Guid accountId, CancellationToken cancellationToken);

    Task<bool> MarkReadAsync(
        Guid accountId,
        Guid notificationId,
        CancellationToken cancellationToken);

    Task<int> ProcessSocialEventsAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationPushWorkItem>> ClaimPushDeliveriesAsync(
        int batchSize,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task RecordPushResponseAsync(
        NotificationPushWorkItem workItem,
        ExpoPushResponse response,
        int maxAttempts,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<NotificationReceiptWorkItem>> ClaimReceiptChecksAsync(
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task RecordReceiptResponseAsync(
        NotificationReceiptWorkItem workItem,
        ExpoReceiptResponse response,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}
