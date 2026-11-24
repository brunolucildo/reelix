namespace Reelix.Api.Notifications.Contracts;

/// <summary>HTTP body of <c>PUT /api/v1/notifications/preferences</c>.</summary>
public sealed record UpdateNotificationPreferencesRequest(
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);

/// <summary>HTTP contract of <c>GET</c> and <c>PUT /api/v1/notifications/preferences</c>.</summary>
public sealed record NotificationPreferencesResponse(
    bool SocialEnabled,
    bool NewEpisodeEnabled,
    bool PremiereEnabled);
