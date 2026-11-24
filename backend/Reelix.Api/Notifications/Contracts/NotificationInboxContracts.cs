using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Contracts;

/// <summary>HTTP contract of <c>GET /api/v1/notifications/inbox</c>.</summary>
public sealed record NotificationInboxResponse(
    IReadOnlyList<NotificationInboxItemResponse> Items,
    string? NextCursor)
{
    public static NotificationInboxResponse From(GetNotificationInboxResult result) =>
        new(result.Items.Select(NotificationInboxItemResponse.From).ToArray(), result.NextCursor);
}

public sealed record NotificationInboxItemResponse(
    Guid Id,
    NotificationCategoryKind Category,
    string Title,
    string Body,
    string DeepLink,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReadAt)
{
    public static NotificationInboxItemResponse From(NotificationInboxItem item) =>
        new(item.Id, item.Category, item.Title, item.Body, item.DeepLink, item.CreatedAt, item.ReadAt);
}

/// <summary>HTTP contract of <c>GET /api/v1/notifications/inbox/unread-count</c>.</summary>
public sealed record NotificationUnreadCountResponse(int Count);
