using Reelix.Application.Authentication;
using Reelix.Application.Errors;
using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

/// <summary>
/// <c>POST /notifications/devices</c>: registers (or refreshes) the push device of an installation.
/// The feature must be on and the project must be the configured one; a token or an installation
/// that is still bound elsewhere is a conflict.
/// </summary>
public sealed class RegisterNotificationDeviceUseCase(
    IUserAccountRepository accounts,
    IPushRegistrationConfiguration pushConfiguration,
    INotificationDevices devices,
    INotificationDeliveries deliveries,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider clock)
{
    public async Task<RegisterNotificationDeviceResult> ExecuteAsync(
        RegisterNotificationDeviceCommand command,
        CancellationToken cancellationToken)
    {
        if (command.ExpoPushToken is null)
        {
            throw new ValidationError("Informe um token Expo.");
        }

        if (!pushConfiguration.IsEnabled ||
            pushConfiguration.ProjectId is null ||
            command.ProjectId != pushConfiguration.ProjectId)
        {
            throw new ServiceUnavailableError(
                "O registro push está desabilitado até configurar o projeto e as credenciais Expo.",
                "Notificações push indisponíveis.");
        }

        var account = await accounts.EnsureAsync(command.SubjectId, cancellationToken);
        var platform = command.Platform.ToDomain();
        var now = clock.GetUtcNow();

        var device = await devices.FindByInstallationAsync(command.InstallationId, cancellationToken);
        if (await devices.TokenInUseAsync(command.ExpoPushToken, device?.Id, cancellationToken))
        {
            throw new NotificationDeviceConflictException(
                "Este token já está associado a outro dispositivo. Remova o vínculo antigo antes de registrar novamente.");
        }

        if (device is not null && device.IsActive && device.AccountId != account.Id)
        {
            throw new NotificationDeviceConflictException(
                "Este dispositivo ainda está associado a outra conta. Encerre a sessão anterior com conexão e tente novamente.");
        }

        if (device is null)
        {
            device = NotificationDevice.Create(
                account.Id,
                command.InstallationId,
                command.ExpoPushToken,
                platform,
                command.ProjectId,
                now);
            devices.Add(device);
        }
        else
        {
            device.RegisterFor(account.Id, command.ExpoPushToken, platform, command.ProjectId, now);
            foreach (var delivery in await deliveries.ListOpenForDeviceAsync(device.Id, cancellationToken))
            {
                delivery.Cancel(NotificationErrorCodes.AccountChanged, now);
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new RegisterNotificationDeviceResult(
            device.Id,
            device.Platform.ToApplication(),
            device.CreatedAt,
            device.LastRegisteredAt);
    }
}
