using Reelix.Api.Authentication;
using Reelix.Api.Notifications.Contracts;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Handlers;

/// <summary>Handler of <c>POST /api/v1/notifications/devices</c>.</summary>
internal static class RegisterNotificationDeviceHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        RegisterNotificationDeviceRequest request,
        RegisterNotificationDeviceUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new RegisterNotificationDeviceCommand(
                subjectId,
                request.InstallationId,
                request.ExpoPushToken,
                request.Platform,
                request.ProjectId),
            cancellationToken);
        return Results.Created(
            $"/api/v1/notifications/devices/{result.Id}",
            NotificationDeviceResponse.From(result));
    }
}
