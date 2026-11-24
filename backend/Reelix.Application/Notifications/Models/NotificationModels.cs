using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

/// <summary>
/// Mirror of the Domain platform of a device, so that no Domain type reaches a use case command or
/// the HTTP contract. Names and values must match the Domain enum (a test guards it).
/// </summary>
public enum DevicePlatform
{
    Ios,
    Android,
}

/// <summary>Mirror of the Domain notification category (same rule as <see cref="DevicePlatform"/>).</summary>
public enum NotificationCategoryKind
{
    Social,
    NewEpisode,
    Premiere,
}

/// <summary>Conversions between the Domain enums and their mirrors (same names, same values).</summary>
public static class NotificationMirrors
{
    public static DevicePlatform ToApplication(this NotificationDevicePlatform platform) =>
        (DevicePlatform)(int)platform;

    /// <summary>
    /// An undefined value stays undefined, so the Domain still rejects it with its own message.
    /// </summary>
    public static NotificationDevicePlatform ToDomain(this DevicePlatform platform) =>
        (NotificationDevicePlatform)(int)platform;

    public static NotificationCategoryKind ToApplication(this NotificationCategory category) =>
        (NotificationCategoryKind)(int)category;
}

/// <summary>One notification of the inbox of an account.</summary>
public sealed record NotificationInboxItem(
    Guid Id,
    NotificationCategoryKind Category,
    string Title,
    string Body,
    string DeepLink,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt);

/// <summary>The position of the last item of an inbox page, used to ask for the next one.</summary>
public sealed record NotificationCursor(DateTimeOffset CreatedAt, Guid Id);

/// <summary>What the push provider is asked to deliver.</summary>
public sealed record PushMessage(
    string PushToken,
    Guid NotificationId,
    string Title,
    string Body,
    string DeepLink);

/// <summary>A pending delivery with the rows its eligibility depends on (any of them can be missing).</summary>
public sealed record PendingPushDelivery(
    NotificationDelivery Delivery,
    UserNotification? Notification,
    NotificationDevice? Device,
    NotificationPreferences? Preferences);

/// <summary>The answer of the push provider to a send, in the words of this application.</summary>
public sealed record PushSendResult(
    bool Accepted,
    string? TicketId,
    string? ErrorCode,
    TimeSpan? RetryAfter,
    bool DeviceNotRegistered,
    bool Retryable);

/// <summary>The answer of the push provider to a receipt check, in the words of this application.</summary>
public sealed record PushReceiptResult(
    bool Ready,
    bool Delivered,
    string? ErrorCode,
    bool DeviceNotRegistered);
