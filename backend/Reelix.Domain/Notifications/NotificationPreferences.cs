namespace Reelix.Domain.Notifications;

public sealed class NotificationPreferences
{
    private NotificationPreferences()
    {
    }

    private NotificationPreferences(Guid accountId)
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; private set; }

    public bool SocialEnabled { get; private set; }

    public bool NewEpisodeEnabled { get; private set; }

    public bool PremiereEnabled { get; private set; }

    public static NotificationPreferences Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta válida.", nameof(accountId));
        }

        return new NotificationPreferences(accountId);
    }

    public void Update(bool socialEnabled, bool newEpisodeEnabled, bool premiereEnabled)
    {
        SocialEnabled = socialEnabled;
        NewEpisodeEnabled = newEpisodeEnabled;
        PremiereEnabled = premiereEnabled;
    }

    public bool IsEnabled(NotificationCategory category) =>
        category switch
        {
            NotificationCategory.Social => SocialEnabled,
            NotificationCategory.NewEpisode => NewEpisodeEnabled,
            NotificationCategory.Premiere => PremiereEnabled,
            _ => throw new ArgumentOutOfRangeException(nameof(category)),
        };
}
