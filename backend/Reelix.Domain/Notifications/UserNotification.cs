namespace Reelix.Domain.Notifications;

public sealed class UserNotification
{
    private UserNotification()
    {
    }

    private UserNotification(
        Guid id,
        Guid accountId,
        NotificationCategory category,
        string sourceKey,
        string title,
        string body,
        string deepLink,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        Category = category;
        SourceKey = sourceKey;
        Title = title;
        Body = body;
        DeepLink = deepLink;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public NotificationCategory Category { get; private set; }

    public string SourceKey { get; private set; } = string.Empty;

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public string DeepLink { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? ReadAt { get; private set; }

    public static UserNotification Create(
        Guid accountId,
        NotificationCategory category,
        string sourceKey,
        string title,
        string body,
        string deepLink)
    {
        if (accountId == Guid.Empty ||
            !Enum.IsDefined(category) ||
            string.IsNullOrWhiteSpace(sourceKey) || sourceKey.Length > 160 ||
            string.IsNullOrWhiteSpace(title) || title.Length > 100 ||
            string.IsNullOrWhiteSpace(body) || body.Length > 240 ||
            !IsSafeDeepLink(deepLink))
        {
            throw new ArgumentException("Os dados da notificação são inválidos.");
        }

        return new UserNotification(
            Guid.NewGuid(),
            accountId,
            category,
            sourceKey,
            title.Trim(),
            body.Trim(),
            deepLink,
            DateTimeOffset.UtcNow);
    }

    public void MarkRead()
    {
        ReadAt ??= DateTimeOffset.UtcNow;
    }

    private static bool IsSafeDeepLink(string? deepLink) =>
        deepLink is "/social" or "/notifications";
}
