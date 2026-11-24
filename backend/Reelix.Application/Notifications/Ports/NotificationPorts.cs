using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.Application.Notifications;

/// <summary>The push feature switch and the project it is registered for.</summary>
public interface IPushRegistrationConfiguration
{
    bool IsEnabled { get; }

    Guid? ProjectId { get; }

    int MaxAttempts { get; }
}

/// <summary>The notification preferences of an account. Entities are tracked so changes can be saved.</summary>
public interface INotificationPreferencesStore
{
    /// <summary>The saved preferences, or null when the account never saved any (nothing is written).</summary>
    Task<NotificationPreferences?> FindAsync(Guid accountId, CancellationToken cancellationToken);

    void Add(NotificationPreferences preferences);
}

/// <summary>The registration of push devices.</summary>
public interface INotificationDevices
{
    Task<NotificationDevice?> FindByInstallationAsync(Guid installationId, CancellationToken cancellationToken);

    /// <summary>Whether an active device other than <paramref name="exceptDeviceId"/> already uses the token.</summary>
    Task<bool> TokenInUseAsync(
        string expoPushToken,
        Guid? exceptDeviceId,
        CancellationToken cancellationToken);

    /// <summary>The active device of the account, or null when it is not theirs or is already removed.</summary>
    Task<NotificationDevice?> FindActiveOwnedAsync(
        Guid accountId,
        Guid deviceId,
        CancellationToken cancellationToken);

    void Add(NotificationDevice device);
}

/// <summary>Looks devices up for the workers.</summary>
public interface INotificationDeviceLookup
{
    Task<IReadOnlyList<NotificationDevice>> ListActiveAsync(Guid accountId, CancellationToken cancellationToken);

    Task<NotificationDevice?> FindAsync(Guid deviceId, CancellationToken cancellationToken);
}

/// <summary>Finds deliveries to change; the entities are tracked so changes can be saved.</summary>
public interface INotificationDeliveries
{
    /// <summary>Pending deliveries of the account whose notification belongs to one of the categories.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListPendingForCategoriesAsync(
        Guid accountId,
        IReadOnlyCollection<NotificationCategory> categories,
        CancellationToken cancellationToken);

    /// <summary>Pending or ticketed deliveries of a device.</summary>
    Task<IReadOnlyList<NotificationDelivery>> ListOpenForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken);

    /// <summary>The delivery held by the given lease, or null when the lease is gone.</summary>
    Task<NotificationDelivery?> FindLeasedAsync(
        Guid deliveryId,
        Guid leaseId,
        CancellationToken cancellationToken);

    /// <summary>The delivery still waiting for the receipt of the given ticket.</summary>
    Task<NotificationDelivery?> FindTicketedAsync(
        Guid deliveryId,
        string ticketId,
        CancellationToken cancellationToken);
}

/// <summary>The inbox of an account.</summary>
public interface IUserNotifications
{
    /// <summary>A page of the inbox, newest first, starting after the cursor.</summary>
    Task<IReadOnlyList<NotificationInboxItem>> ListPageAsync(
        Guid accountId,
        NotificationCursor? cursor,
        int limit,
        CancellationToken cancellationToken);

    Task<int> CountUnreadAsync(Guid accountId, CancellationToken cancellationToken);

    /// <summary>The notification of the account, or null when it is not theirs or does not exist.</summary>
    Task<UserNotification?> FindOwnedAsync(
        Guid accountId,
        Guid notificationId,
        CancellationToken cancellationToken);
}

/// <summary>
/// The relationship between two accounts, read for the social event decision. The friend table
/// belongs to Social; this read keeps going to it directly until Social exposes a port (spec 0016).
/// </summary>
public interface IFriendships
{
    Task<FriendRelationship?> FindBetweenAsync(
        Guid firstAccountId,
        Guid secondAccountId,
        CancellationToken cancellationToken);
}

/// <summary>Saves the changes made to tracked entities.</summary>
public interface INotificationsUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

/// <summary>The outbox of social events waiting to become notifications.</summary>
public interface ISocialEventOutbox
{
    /// <summary>
    /// Reserves up to <paramref name="batchSize"/> unprocessed events, oldest first. Another
    /// worker never reserves the same events until the batch is committed or abandoned.
    /// </summary>
    Task<ISocialEventBatch> ReserveAsync(int batchSize, CancellationToken cancellationToken);
}

/// <summary>A reserved batch of social events; leaving it without committing releases the events.</summary>
public interface ISocialEventBatch : IAsyncDisposable
{
    IReadOnlyList<SocialEvent> Events { get; }

    /// <summary>Records the decision for one event: its receipt, and the notification with its deliveries when there is one.</summary>
    void Stage(SocialEventPlan plan, ProcessedNotificationEvent receipt);

    /// <summary>Saves everything staged and releases the events in one transaction.</summary>
    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>The queues the push worker reads from.</summary>
public interface IPushDeliveryQueue
{
    /// <summary>Reserves up to <paramref name="batchSize"/> deliveries due at <paramref name="now"/> and not leased.</summary>
    Task<IPushDeliveryBatch> ReservePendingAsync(
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>Reserves up to <paramref name="batchSize"/> ticketed deliveries whose receipt is due at <paramref name="now"/>.</summary>
    Task<IReceiptCheckBatch> ReserveReceiptChecksAsync(
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken);
}

/// <summary>A reserved batch of pending deliveries; the entities are tracked and saved on commit.</summary>
public interface IPushDeliveryBatch : IAsyncDisposable
{
    IReadOnlyList<PendingPushDelivery> Items { get; }

    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>A reserved batch of ticketed deliveries; the entities are tracked and saved on commit.</summary>
public interface IReceiptCheckBatch : IAsyncDisposable
{
    IReadOnlyList<NotificationDelivery> Deliveries { get; }

    Task CommitAsync(CancellationToken cancellationToken);
}

/// <summary>Sends push messages. Named by the need, not by the provider behind it.</summary>
public interface IPushSender
{
    /// <summary>
    /// Sends one message. A transport failure (network error, timeout, unreadable answer) is
    /// reported as a retryable <see cref="PushSendResult"/>; only a cancelled request throws.
    /// </summary>
    Task<PushSendResult> SendAsync(PushMessage message, CancellationToken cancellationToken);

    /// <summary>
    /// Reads the receipt of a ticket. A transport failure propagates: the caller's cycle stops and
    /// the worker repeats it.
    /// </summary>
    Task<PushReceiptResult> GetReceiptAsync(string ticketId, CancellationToken cancellationToken);
}

/// <summary>The source of the random number that spreads retries apart.</summary>
public interface IJitterSource
{
    /// <returns>A number in [0, 1).</returns>
    double NextUnit();
}
