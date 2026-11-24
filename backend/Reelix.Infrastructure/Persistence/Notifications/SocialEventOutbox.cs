using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.Infrastructure.Persistence.Notifications;

/// <summary>
/// Reserves unprocessed social events inside a transaction. The rows stay locked for other workers
/// until the batch is committed (or abandoned), which is what keeps two workers from handling one
/// event twice. The event table belongs to Social and is read here until Social exposes a port
/// (spec 0016).
/// </summary>
public sealed class SocialEventOutbox(ReelixDbContext dbContext) : ISocialEventOutbox
{
    public async Task<ISocialEventBatch> ReserveAsync(int batchSize, CancellationToken cancellationToken)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var events = await dbContext.SocialEvents
                .FromSqlInterpolated($"""
                    SELECT s.*
                    FROM public.social_events AS s
                    WHERE NOT EXISTS (
                        SELECT 1
                        FROM public.notification_processed_social_events AS p
                        WHERE p.event_id = s.id)
                    ORDER BY s.occurred_at, s.id
                    LIMIT {batchSize}
                    FOR UPDATE SKIP LOCKED
                    """)
                .ToArrayAsync(cancellationToken);
            return new Batch(dbContext, transaction, events);
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    private sealed class Batch(
        ReelixDbContext dbContext,
        IDbContextTransaction transaction,
        IReadOnlyList<SocialEvent> events) : ISocialEventBatch
    {
        public IReadOnlyList<SocialEvent> Events => events;

        public void Stage(SocialEventPlan plan, ProcessedNotificationEvent receipt)
        {
            if (plan.Notification is not null)
            {
                dbContext.UserNotifications.Add(plan.Notification);
                dbContext.NotificationDeliveries.AddRange(plan.Deliveries);
            }

            dbContext.ProcessedNotificationEvents.Add(receipt);
        }

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
