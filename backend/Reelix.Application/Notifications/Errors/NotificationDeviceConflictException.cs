using Reelix.Application.Errors;

namespace Reelix.Application.Notifications;

/// <summary>The device or its token is already bound to another device or account (HTTP 409).</summary>
public sealed class NotificationDeviceConflictException(string message)
    : ConflictError(message, "Dispositivo já associado.");
