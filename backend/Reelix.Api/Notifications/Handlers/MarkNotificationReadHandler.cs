using Reelix.Api.Authentication;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Handlers;

/// <summary>Handler of <c>PUT /api/v1/notifications/inbox/{notificationId}/read</c>.</summary>
internal static class MarkNotificationReadHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        Guid notificationId,
        MarkNotificationReadUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new MarkNotificationReadCommand(subjectId, notificationId),
            cancellationToken);
        return result.Marked ? Results.NoContent() : Results.NotFound();
    }
}
