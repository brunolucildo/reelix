namespace Reelix.Application.Notifications;

public sealed record UpdateNotificationPreferencesCommand(
    Guid SubjectId,
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);
