using Reelix.Api.Authentication;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Handlers;

/// <summary>Handler of <c>DELETE /api/v1/notifications/devices/{deviceId}</c>.</summary>
internal static class DeactivateNotificationDeviceHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        Guid deviceId,
        DeactivateNotificationDeviceUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new DeactivateNotificationDeviceCommand(subjectId, deviceId),
            cancellationToken);
        return result.Deactivated ? Results.NoContent() : Results.NotFound();
    }
}
