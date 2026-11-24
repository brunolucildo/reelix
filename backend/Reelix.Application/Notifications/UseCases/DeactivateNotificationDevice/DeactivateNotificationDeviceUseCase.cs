using Reelix.Application.Authentication;
using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

/// <summary>
/// <c>DELETE /notifications/devices/{id}</c>: deactivates a device of the caller and cancels its
/// open deliveries. It never creates an account: a caller without one has no device to remove.
/// </summary>
public sealed class DeactivateNotificationDeviceUseCase(
    IUserAccountRepository accounts,
    INotificationDevices devices,
    INotificationDeliveries deliveries,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<DeactivateNotificationDeviceResult> ExecuteAsync(
        DeactivateNotificationDeviceCommand command,
        CancellationToken cancellationToken)
    {
        var account = await accounts.FindAsync(command.SubjectId, cancellationToken);
        if (account is null)
        {
            return new DeactivateNotificationDeviceResult(false);
        }

        var device = await devices.FindActiveOwnedAsync(account.Id, command.DeviceId, cancellationToken);
        if (device is null)
        {
            return new DeactivateNotificationDeviceResult(false);
        }

        device.Deactivate();
        var now = clock.GetUtcNow();
        foreach (var delivery in await deliveries.ListOpenForDeviceAsync(device.Id, cancellationToken))
        {
            delivery.Cancel(NotificationErrorCodes.DeviceRemoved, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new DeactivateNotificationDeviceResult(true);
    }
}
