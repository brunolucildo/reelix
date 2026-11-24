using Reelix.Application.Authentication;
using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

/// <summary>
/// <c>PUT /notifications/preferences</c>: saves the categories the account wants and cancels, in the
/// same save, the pending deliveries of every category that is now off.
/// </summary>
public sealed class UpdateNotificationPreferencesUseCase(
    IUserAccountRepository accounts,
    INotificationPreferencesStore preferences,
    INotificationDeliveries deliveries,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider clock)
{
    private static readonly NotificationCategory[] Categories = Enum.GetValues<NotificationCategory>();

    public async Task<UpdateNotificationPreferencesResult> ExecuteAsync(
        UpdateNotificationPreferencesCommand command,
        CancellationToken cancellationToken)
    {
        var account = await accounts.EnsureAsync(command.SubjectId, cancellationToken);
        var current = await preferences.FindAsync(account.Id, cancellationToken);
        if (current is null)
        {
            current = NotificationPreferences.Create(account.Id);
            preferences.Add(current);
        }

        current.Update(command.SocialEnabled, command.NewEpisodeEnabled, command.PremiereEnabled);

        var turnedOff = Categories.Where(category => !current.IsEnabled(category)).ToArray();
        var pending = await deliveries.ListPendingForCategoriesAsync(account.Id, turnedOff, cancellationToken);
        var now = clock.GetUtcNow();
        foreach (var delivery in pending)
        {
            delivery.Cancel(NotificationErrorCodes.PreferenceDisabled, now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new UpdateNotificationPreferencesResult(
            current.SocialEnabled,
            current.NewEpisodeEnabled,
            current.PremiereEnabled);
    }
}
