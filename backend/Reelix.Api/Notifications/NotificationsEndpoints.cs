using Microsoft.AspNetCore.RateLimiting;
using Reelix.Api.Errors;
using Reelix.Api.Notifications.Contracts;
using Reelix.Api.Notifications.Handlers;

namespace Reelix.Api.Notifications;

public static class NotificationsEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/preferences", GetNotificationPreferencesHandler.HandleAsync)
            .WithName("GetNotificationPreferences")
            .WithApplicationProblems()
            .Produces<NotificationPreferencesResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/preferences", UpdateNotificationPreferencesHandler.HandleAsync)
            .WithName("UpdateNotificationPreferences")
            .WithApplicationProblems()
            .RequireRateLimiting("notifications-write")
            .Produces<NotificationPreferencesResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapPost("/devices", RegisterNotificationDeviceHandler.HandleAsync)
            .WithName("RegisterNotificationDevice")
            .WithApplicationProblems(
                new ApplicationProblemOptions { ValidationTitle = "Registro de dispositivo inválido." })
            .RequireRateLimiting("notifications-write")
            .Produces<NotificationDeviceResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapDelete("/devices/{deviceId:guid}", DeactivateNotificationDeviceHandler.HandleAsync)
            .WithName("DeactivateNotificationDevice")
            .WithApplicationProblems()
            .RequireRateLimiting("notifications-write")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapGet("/inbox", GetNotificationInboxHandler.HandleAsync)
            .WithName("GetNotificationInbox")
            .WithApplicationProblems(new ApplicationProblemOptions { ValidationTitle = "Cursor inválido." })
            .Produces<NotificationInboxResponse>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapGet("/inbox/unread-count", GetNotificationUnreadCountHandler.HandleAsync)
            .WithName("GetNotificationUnreadCount")
            .WithApplicationProblems()
            .Produces<NotificationUnreadCountResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/inbox/{notificationId:guid}/read", MarkNotificationReadHandler.HandleAsync)
            .WithName("MarkNotificationRead")
            .WithApplicationProblems()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return group;
    }
}
