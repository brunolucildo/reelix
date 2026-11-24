namespace Reelix.Application.Notifications;

public sealed record UpdateNotificationPreferencesResult(
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);
