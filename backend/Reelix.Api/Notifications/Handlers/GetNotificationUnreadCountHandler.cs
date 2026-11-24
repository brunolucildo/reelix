using Reelix.Api.Authentication;
using Reelix.Api.Notifications.Contracts;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Handlers;

/// <summary>Handler of <c>GET /api/v1/notifications/inbox/unread-count</c>.</summary>
internal static class GetNotificationUnreadCountHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        GetNotificationUnreadCountUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new GetNotificationUnreadCountQuery(subjectId),
            cancellationToken);
        return Results.Ok(new NotificationUnreadCountResponse(result.Count));
    }
}
