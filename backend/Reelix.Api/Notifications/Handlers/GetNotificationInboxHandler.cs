using Reelix.Api.Authentication;
using Reelix.Api.Notifications.Contracts;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications.Handlers;

/// <summary>Handler of <c>GET /api/v1/notifications/inbox</c>.</summary>
internal static class GetNotificationInboxHandler
{
    public static async Task<IResult> HandleAsync(
        ICurrentUser currentUser,
        string? cursor,
        GetNotificationInboxUseCase useCase,
        CancellationToken cancellationToken)
    {
        if (currentUser.SubjectId is not { } subjectId)
        {
            return Results.Unauthorized();
        }

        var result = await useCase.ExecuteAsync(
            new GetNotificationInboxQuery(subjectId, cursor),
            cancellationToken);
        return Results.Ok(NotificationInboxResponse.From(result));
    }
}
