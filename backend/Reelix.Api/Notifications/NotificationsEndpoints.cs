using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;
using Reelix.Application.Notifications;

namespace Reelix.Api.Notifications;

public static class NotificationsEndpoints
{
    public static RouteGroupBuilder MapNotificationsEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        group.MapGet("/preferences", GetPreferences)
            .WithName("GetNotificationPreferences")
            .Produces<NotificationPreferencesData>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/preferences", UpdatePreferences)
            .WithName("UpdateNotificationPreferences")
            .RequireRateLimiting("notifications-write")
            .Produces<NotificationPreferencesData>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapPost("/devices", RegisterDevice)
            .WithName("RegisterNotificationDevice")
            .RequireRateLimiting("notifications-write")
            .Produces<NotificationDeviceData>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapDelete("/devices/{deviceId:guid}", DeactivateDevice)
            .WithName("DeactivateNotificationDevice")
            .RequireRateLimiting("notifications-write")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status429TooManyRequests);
        group.MapGet("/inbox", GetInbox)
            .WithName("GetNotificationInbox")
            .Produces<NotificationInboxPage>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);
        group.MapPut("/inbox/{notificationId:guid}/read", MarkRead)
            .WithName("MarkNotificationRead")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
        return group;
    }

    private static async Task<IResult> GetPreferences(
        HttpContext context,
        NotificationsService service,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await service.GetPreferencesAsync(userId, cancellationToken));
    }

    private static async Task<IResult> UpdatePreferences(
        HttpContext context,
        NotificationsService service,
        UpdateNotificationPreferences preferences,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return Results.Ok(await service.UpdatePreferencesAsync(userId, preferences, cancellationToken));
    }

    private static async Task<IResult> RegisterDevice(
        HttpContext context,
        NotificationsService service,
        RegisterNotificationDevice registration,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            var device = await service.RegisterDeviceAsync(userId, registration, cancellationToken);
            return Results.Created($"/api/v1/notifications/devices/{device.Id}", device);
        }
        catch (NotificationDeviceConflictException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Dispositivo já associado.",
                detail: exception.Message);
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Registro de dispositivo inválido.",
                detail: exception.Message);
        }
        catch (InvalidOperationException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status503ServiceUnavailable,
                title: "Notificações push indisponíveis.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> DeactivateDevice(
        HttpContext context,
        NotificationsService service,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await service.DeactivateDeviceAsync(userId, deviceId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static async Task<IResult> GetInbox(
        HttpContext context,
        NotificationsService service,
        string? cursor,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        try
        {
            return Results.Ok(await service.GetInboxAsync(userId, cursor, cancellationToken));
        }
        catch (ArgumentException exception)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Cursor inválido.",
                detail: exception.Message);
        }
    }

    private static async Task<IResult> MarkRead(
        HttpContext context,
        NotificationsService service,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(context, out var userId))
        {
            return Results.Unauthorized();
        }

        return await service.MarkReadAsync(userId, notificationId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
    }

    private static bool TryGetUserId(HttpContext context, out Guid userId) =>
        Guid.TryParse(context.User.FindFirstValue("sub"), out userId) && userId != Guid.Empty;
}
