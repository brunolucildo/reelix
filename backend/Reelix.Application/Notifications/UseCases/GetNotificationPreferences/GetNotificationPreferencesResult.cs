namespace Reelix.Application.Notifications;

public sealed record GetNotificationPreferencesResult(
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);
