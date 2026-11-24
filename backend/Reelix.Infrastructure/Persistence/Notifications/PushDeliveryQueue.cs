using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;

namespace Reelix.Infrastructure.Persistence.Notifications;

/// <summary>
/// Reserves the deliveries a push worker cycle works on. The rows stay locked for other workers
/// until the batch is committed (or abandoned); the decisions about them are the use case's.
/// </summary>
public sealed class PushDeliveryQueue(ReelixDbContext dbContext) : IPushDeliveryQueue
{
    public async Task<IPushDeliveryBatch> ReservePendingAsync(
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var deliveries = await dbContext.NotificationDeliveries
                .FromSqlInterpolated($"""
                    SELECT d.*
                    FROM public.notification_deliveries AS d
                    WHERE d.status = 'Pending'
                        AND d.next_attempt_at <= {now}
                        AND (d.lease_until IS NULL OR d.lease_until <= {now})
                    ORDER BY d.next_attempt_at, d.id
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToArrayAsync(cancellationToken);

            var notificationIds = deliveries.Select(item => item.NotificationId).Distinct().ToArray();
            var deviceIds = deliveries.Select(item => item.DeviceId).Distinct().ToArray();
            var notifications = await dbContext.UserNotifications
                .Where(item => notificationIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);
            var devices = await dbContext.NotificationDevices
                .Where(item => deviceIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, cancellationToken);
            var accountIds = notifications.Values.Select(item => item.AccountId).Distinct().ToArray();
            var preferences = await dbContext.NotificationPreferences
                .Where(item => accountIds.Contains(item.AccountId))
                .ToDictionaryAsync(item => item.AccountId, cancellationToken);

            var items = deliveries
                .Select(delivery =>
                {
                    notifications.TryGetValue(delivery.NotificationId, out var notification);
                    devices.TryGetValue(delivery.DeviceId, out var device);
                    NotificationPreferences? preference = null;
                    if (notification is not null)
                    {
                        preferences.TryGetValue(notification.AccountId, out preference);
                    }

                    return new PendingPushDelivery(delivery, notification, device, preference);
                })
                .ToArray();
            return new PendingBatch(dbContext, transaction, items);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    public async Task<IReceiptCheckBatch> ReserveReceiptChecksAsync(
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var deliveries = await dbContext.NotificationDeliveries
                .FromSqlInterpolated($"""
                    SELECT d.*
                    FROM public.notification_deliveries AS d
                    WHERE d.status = 'Ticketed'
                        AND d.receipt_check_at <= {now}
                    ORDER BY d.receipt_check_at, d.id
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToArrayAsync(cancellationToken);
            return new ReceiptBatch(dbContext, transaction, deliveries);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class PendingBatch(
        ReelixDbContext dbContext,
        IDbContextTransaction transaction,
        IReadOnlyList<PendingPushDelivery> items) : IPushDeliveryBatch
    {
        public IReadOnlyList<PendingPushDelivery> Items => items;

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    private sealed class ReceiptBatch(
        ReelixDbContext dbContext,
        IDbContextTransaction transaction,
        IReadOnlyList<NotificationDelivery> deliveries) : IReceiptCheckBatch
    {
        public IReadOnlyList<NotificationDelivery> Deliveries => deliveries;

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
