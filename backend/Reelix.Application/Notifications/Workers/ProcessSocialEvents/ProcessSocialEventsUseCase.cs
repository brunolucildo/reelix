using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.Application.Notifications;

/// <summary>
/// One cycle of the social notification worker: reserves a batch of events from the outbox, lets
/// the pure <see cref="SocialEventNotificationPlanner"/> decide each one, and commits the
/// notifications, deliveries and receipts of the whole batch in one transaction. Reprocessing never
/// duplicates (a processed event is not reserved again) and two workers never get the same event.
/// </summary>
public sealed class ProcessSocialEventsUseCase(
    ISocialEventOutbox outbox,
    IFriendships friendships,
    INotificationPreferencesStore preferences,
    INotificationDeviceLookup devices,
    TimeProvider clock)
{
    /// <summary>Events handled per cycle.</summary>
    public const int BatchSize = 100;

    public async Task<ProcessSocialEventsResult> ExecuteAsync(
        ProcessSocialEventsCommand command,
        CancellationToken cancellationToken)
    {
        await using var batch = await outbox.ReserveAsync(BatchSize, cancellationToken);
        foreach (var socialEvent in batch.Events)
        {
            var relationship = await friendships.FindBetweenAsync(
                socialEvent.ActorAccountId,
                socialEvent.RecipientAccountId,
                cancellationToken);
            var recipientPreferences = await preferences.FindAsync(
                socialEvent.RecipientAccountId,
                cancellationToken);
            var recipientDevices = await devices.ListActiveAsync(
                socialEvent.RecipientAccountId,
                cancellationToken);

            var now = clock.GetUtcNow();
            var plan = SocialEventNotificationPlanner.Plan(
                socialEvent,
                relationship,
                recipientPreferences,
                recipientDevices,
                now);
            batch.Stage(plan, ProcessedNotificationEvent.Create(socialEvent.Id, plan.Outcome, now));
        }

        await batch.CommitAsync(cancellationToken);
        return new ProcessSocialEventsResult(batch.Events.Count);
    }
}
