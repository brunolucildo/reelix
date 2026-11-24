using Reelix.Api.Authentication;
using Reelix.Api.Notifications.Contracts;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Handlers;

/// <summary>Handler of <c>PUT /api/v1/notifications/preferences</c>.</summary>
internal static class UpdateNotificationPreferencesHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        UpdateNotificationPreferencesRequest request,
        UpdateNotificationPreferencesUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new UpdateNotificationPreferencesCommand(
                subjectId,
                request.SocialEnabled,
                request.NewEpisodeEnabled,
                request.PremiereEnabled),
            cancellationToken);
        return Results.Ok(new NotificationPreferencesResponse(
            result.SocialEnabled,
            result.NewEpisodeEnabled,
            result.PremiereEnabled));
    }
}
