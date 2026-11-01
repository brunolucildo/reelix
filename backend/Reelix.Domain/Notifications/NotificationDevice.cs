namespace Reelix.Domain.Notifications;

public enum NotificationDevicePlatform
{
    Ios,
    Android,
}

public sealed class NotificationDevice
{
    private NotificationDevice()
    {
    }

    private NotificationDevice(
        Guid id,
        Guid accountId,
        Guid installationId,
        string expoPushToken,
        NotificationDevicePlatform platform,
        Guid projectId,
        DateTimeOffset createdAt)
    {
        Id = id;
        AccountId = accountId;
        InstallationId = installationId;
        ExpoPushToken = expoPushToken;
        Platform = platform;
        ProjectId = projectId;
        IsActive = true;
        CreatedAt = createdAt;
        LastRegisteredAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid AccountId { get; private set; }

    public Guid InstallationId { get; private set; }

    public string ExpoPushToken { get; private set; } = string.Empty;

    public NotificationDevicePlatform Platform { get; private set; }

    public Guid ProjectId { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset LastRegisteredAt { get; private set; }

    public static NotificationDevice Create(
        Guid accountId,
        Guid installationId,
        string expoPushToken,
        NotificationDevicePlatform platform,
        Guid projectId)
    {
        Validate(accountId, installationId, expoPushToken, platform, projectId);
        var now = DateTimeOffset.UtcNow;
        return new NotificationDevice(
            Guid.NewGuid(),
            accountId,
            installationId,
            expoPushToken,
            platform,
            projectId,
            now);
    }

    public void RegisterFor(
        Guid accountId,
        string expoPushToken,
        NotificationDevicePlatform platform,
        Guid projectId)
    {
        if (IsActive && AccountId != accountId)
        {
            throw new InvalidOperationException(
                "Um dispositivo ativo não pode ser transferido entre contas sem revogação.");
        }

        Validate(accountId, InstallationId, expoPushToken, platform, projectId);
        AccountId = accountId;
        ExpoPushToken = expoPushToken;
        Platform = platform;
        ProjectId = projectId;
        IsActive = true;
        LastRegisteredAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;

    private static void Validate(
        Guid accountId,
        Guid installationId,
        string expoPushToken,
        NotificationDevicePlatform platform,
        Guid projectId)
    {
        if (accountId == Guid.Empty || installationId == Guid.Empty || projectId == Guid.Empty)
        {
            throw new ArgumentException("Informe conta, instalação e projeto válidos.");
        }

        if (expoPushToken.Length is < 20 or > 255 ||
            !(expoPushToken.StartsWith("ExpoPushToken[", StringComparison.Ordinal) ||
              expoPushToken.StartsWith("ExponentPushToken[", StringComparison.Ordinal)) ||
            !expoPushToken.EndsWith(']'))
        {
            throw new ArgumentException("O token Expo informado é inválido.", nameof(expoPushToken));
        }

        if (!Enum.IsDefined(platform))
        {
            throw new ArgumentOutOfRangeException(nameof(platform), "A plataforma do dispositivo é inválida.");
        }
    }
}
