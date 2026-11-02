using Microsoft.EntityFrameworkCore;
using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.Infrastructure.Persistence.Notifications;

public sealed class NotificationsRepository(ReelixDbContext dbContext) : INotificationsRepository
{
    public async Task<NotificationPreferences> GetPreferencesAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        var preferences = await dbContext.NotificationPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (preferences is not null)
        {
            return preferences;
        }

        preferences = NotificationPreferences.Create(accountId);
        dbContext.NotificationPreferences.Add(preferences);
        await dbContext.SaveChangesAsync(cancellationToken);
        return preferences;
    }

    public async Task<NotificationPreferences> UpdatePreferencesAsync(
        Guid accountId,
        UpdateNotificationPreferences update,
        CancellationToken cancellationToken)
    {
        var preferences = await dbContext.NotificationPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);
        if (preferences is null)
        {
            preferences = NotificationPreferences.Create(accountId);
            dbContext.NotificationPreferences.Add(preferences);
        }

        preferences.Update(update.SocialEnabled, update.NewEpisodeEnabled, update.PremiereEnabled);
        var pendingDeliveries = await (
            from delivery in dbContext.NotificationDeliveries
            join notification in dbContext.UserNotifications
                on delivery.NotificationId equals notification.Id
            where notification.AccountId == accountId &&
                delivery.Status == NotificationDeliveryStatus.Pending &&
                (notification.Category == NotificationCategory.Social && !update.SocialEnabled ||
                 notification.Category == NotificationCategory.NewEpisode && !update.NewEpisodeEnabled ||
                 notification.Category == NotificationCategory.Premiere && !update.PremiereEnabled)
            select delivery)
            .ToArrayAsync(cancellationToken);
        foreach (var delivery in pendingDeliveries)
        {
            delivery.Cancel("preference_disabled");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return preferences;
    }

    public async Task<NotificationDevice> RegisterDeviceAsync(
        Guid accountId,
        RegisterNotificationDevice registration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var device = await dbContext.NotificationDevices.SingleOrDefaultAsync(
            item => item.InstallationId == registration.InstallationId,
            cancellationToken);
        var duplicateTokenDevice = await dbContext.NotificationDevices.SingleOrDefaultAsync(
            item => item.ExpoPushToken == registration.ExpoPushToken &&
                item.IsActive &&
                (device == null || item.Id != device.Id),
            cancellationToken);
        if (duplicateTokenDevice is not null)
        {
            throw new NotificationDeviceConflictException(
                "Este token já está associado a outro dispositivo. Remova o vínculo antigo antes de registrar novamente.");
        }

        if (device is not null && device.IsActive && device.AccountId != accountId)
        {
            throw new NotificationDeviceConflictException(
                "Este dispositivo ainda está associado a outra conta. Encerre a sessão anterior com conexão e tente novamente.");
        }

        if (device is null)
        {
            device = NotificationDevice.Create(
                accountId,
                registration.InstallationId,
                registration.ExpoPushToken,
                registration.Platform,
                registration.ProjectId);
            dbContext.NotificationDevices.Add(device);
        }
        else
        {
            device.RegisterFor(
                accountId,
                registration.ExpoPushToken,
                registration.Platform,
                registration.ProjectId);
            await CancelDeliveriesAsync(device.Id, "account_changed", cancellationToken);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return device;
    }

    public async Task<bool> DeactivateDeviceAsync(
        Guid accountId,
        Guid deviceId,
        CancellationToken cancellationToken)
    {
        var device = await dbContext.NotificationDevices.SingleOrDefaultAsync(
            item => item.Id == deviceId && item.AccountId == accountId && item.IsActive,
            cancellationToken);
        if (device is null)
        {
            return false;
        }

        device.Deactivate();
        await CancelDeliveriesAsync(device.Id, "device_removed", cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<NotificationInboxItem>> GetInboxPageAsync(
        Guid accountId,
        NotificationCursor? cursor,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = dbContext.UserNotifications
            .AsNoTracking()
            .Where(item => item.AccountId == accountId);
        if (cursor is not null)
        {
            query = query.Where(item =>
                item.CreatedAt < cursor.CreatedAt ||
                item.CreatedAt == cursor.CreatedAt && item.Id.CompareTo(cursor.Id) < 0);
        }

        return await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(limit)
            .Select(item => new NotificationInboxItem(
                item.Id,
                item.Category,
                item.Title,
                item.Body,
                item.DeepLink,
                item.CreatedAt,
                item.ReadAt))
            .ToArrayAsync(cancellationToken);
    }

    public Task<int> GetUnreadCountAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.UserNotifications.CountAsync(
            item => item.AccountId == accountId && item.ReadAt == null,
            cancellationToken);

    public async Task<bool> MarkReadAsync(
        Guid accountId,
        Guid notificationId,
        CancellationToken cancellationToken)
    {
        var notification = await dbContext.UserNotifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.AccountId == accountId,
            cancellationToken);
        if (notification is null)
        {
            return false;
        }

        notification.MarkRead();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> ProcessSocialEventsAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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

        foreach (var socialEvent in events)
        {
            var outcome = await ProcessSocialEventAsync(socialEvent, cancellationToken);
            dbContext.ProcessedNotificationEvents.Add(
                ProcessedNotificationEvent.Create(socialEvent.Id, outcome));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return events.Length;
    }

    public async Task<IReadOnlyList<NotificationPushWorkItem>> ClaimPushDeliveriesAsync(
        int batchSize,
        DateTimeOffset now,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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

        var workItems = new List<NotificationPushWorkItem>();
        foreach (var delivery in deliveries)
        {
            if (!notifications.TryGetValue(delivery.NotificationId, out var notification) ||
                !devices.TryGetValue(delivery.DeviceId, out var device) ||
                !device.IsActive ||
                notification.AccountId != device.AccountId)
            {
                delivery.Cancel("device_or_notification_inactive");
                continue;
            }

            if (!preferences.TryGetValue(notification.AccountId, out var preference) ||
                !preference.IsEnabled(notification.Category))
            {
                delivery.Cancel("preference_disabled");
                continue;
            }

            var leaseId = Guid.NewGuid();
            delivery.Claim(leaseId, now.Add(leaseDuration), now);
            workItems.Add(new NotificationPushWorkItem(
                delivery.Id,
                device.Id,
                leaseId,
                device.ExpoPushToken,
                notification.Id,
                notification.Title,
                notification.Body,
                notification.DeepLink));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return workItems;
    }

    public async Task RecordPushResponseAsync(
        NotificationPushWorkItem workItem,
        ExpoPushResponse response,
        int maxAttempts,
        CancellationToken cancellationToken)
    {
        var delivery = await dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            item => item.Id == workItem.DeliveryId && item.LeaseId == workItem.LeaseId,
            cancellationToken);
        if (delivery is null)
        {
            return;
        }

        if (response.Accepted && response.TicketId is not null)
        {
            delivery.RecordTicket(
                workItem.LeaseId,
                response.TicketId,
                DateTimeOffset.UtcNow.AddMinutes(15));
        }
        else
        {
            var deviceNotRegistered = response.DeviceNotRegistered;
            if (deviceNotRegistered)
            {
                var device = await dbContext.NotificationDevices.SingleOrDefaultAsync(
                    item => item.Id == workItem.DeviceId,
                    cancellationToken);
                device?.Deactivate();
            }

            var exponentialSeconds = Math.Min(
                3600,
                5 * Math.Pow(2, Math.Min(delivery.AttemptCount - 1, 10)));
            var retryDelay = response.RetryAfter ??
                TimeSpan.FromSeconds(exponentialSeconds * (0.8 + Random.Shared.NextDouble() * 0.4));
            delivery.Retry(
                workItem.LeaseId,
                deviceNotRegistered || !response.Retryable ? 1 : maxAttempts,
                DateTimeOffset.UtcNow.Add(retryDelay),
                response.ErrorCode ?? "expo_push_rejected");
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationReceiptWorkItem>> ClaimReceiptChecksAsync(
        int batchSize,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
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
        var workItems = new List<NotificationReceiptWorkItem>(deliveries.Length);
        foreach (var delivery in deliveries)
        {
            if (delivery.ExpoTicketId is null ||
                delivery.ExpoTicketCreatedAt <= now.AddHours(-24))
            {
                delivery.ExpireReceipt();
                continue;
            }

            delivery.RetryReceipt(
                now.AddMinutes(GetReceiptRetryMinutes(delivery.ReceiptCheckCount)),
                "receipt_check_pending");
            workItems.Add(new NotificationReceiptWorkItem(
                delivery.Id,
                delivery.DeviceId,
                delivery.ExpoTicketId));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return workItems;
    }

    public async Task RecordReceiptResponseAsync(
        NotificationReceiptWorkItem workItem,
        ExpoReceiptResponse response,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var delivery = await dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            item => item.Id == workItem.DeliveryId &&
                item.ExpoTicketId == workItem.TicketId &&
                item.Status == NotificationDeliveryStatus.Ticketed,
            cancellationToken);
        if (delivery is null)
        {
            return;
        }

        if (!response.Ready)
        {
            delivery.RetryReceipt(
                now.AddMinutes(GetReceiptRetryMinutes(delivery.ReceiptCheckCount)),
                response.ErrorCode ?? "receipt_not_ready");
        }
        else
        {
            if (response.DeviceNotRegistered)
            {
                var device = await dbContext.NotificationDevices.SingleOrDefaultAsync(
                    item => item.Id == workItem.DeviceId,
                    cancellationToken);
                device?.Deactivate();
            }

            delivery.CompleteReceipt(response.Delivered, response.ErrorCode);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static double GetReceiptRetryMinutes(int checkCount) =>
        Math.Min(360, 15 * Math.Pow(2, Math.Min(checkCount, 5)));

    private async Task<string> ProcessSocialEventAsync(
        SocialEvent socialEvent,
        CancellationToken cancellationToken)
    {
        var expectedStatus = socialEvent.EventType switch
        {
            "friend.requested" => FriendRelationshipStatus.Pending,
            "friend.accepted" => FriendRelationshipStatus.Accepted,
            _ => (FriendRelationshipStatus?)null,
        };
        if (expectedStatus is null)
        {
            return "obsolete";
        }

        var relationship = await dbContext.FriendRelationships
            .AsNoTracking()
            .SingleOrDefaultAsync(item =>
                item.Status == expectedStatus &&
                (item.RequesterAccountId == socialEvent.ActorAccountId &&
                 item.AddresseeAccountId == socialEvent.RecipientAccountId ||
                 item.RequesterAccountId == socialEvent.RecipientAccountId &&
                 item.AddresseeAccountId == socialEvent.ActorAccountId),
                cancellationToken);
        if (relationship is null ||
            socialEvent.EventType == "friend.requested" &&
            (relationship.RequesterAccountId != socialEvent.ActorAccountId ||
             relationship.AddresseeAccountId != socialEvent.RecipientAccountId))
        {
            return "obsolete";
        }

        var preferences = await dbContext.NotificationPreferences
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.AccountId == socialEvent.RecipientAccountId,
                cancellationToken);
        if (preferences is null || !preferences.IsEnabled(NotificationCategory.Social))
        {
            return "disabled";
        }

        var (title, body) = socialEvent.EventType switch
        {
            "friend.requested" => (
                "Nova solicitação de amizade",
                "Você recebeu uma solicitação de amizade."),
            "friend.accepted" => (
                "Solicitação aceita",
                "Sua solicitação de amizade foi aceita."),
            _ => throw new InvalidOperationException("Tipo de evento social inesperado."),
        };
        var notification = UserNotification.Create(
            socialEvent.RecipientAccountId,
            NotificationCategory.Social,
            $"social:{socialEvent.Id}",
            title,
            body,
            "/social");
        dbContext.UserNotifications.Add(notification);

        var devices = await dbContext.NotificationDevices
            .Where(device => device.AccountId == socialEvent.RecipientAccountId && device.IsActive)
            .ToArrayAsync(cancellationToken);
        dbContext.NotificationDeliveries.AddRange(
            devices.Select(device => NotificationDelivery.Create(notification.Id, device.Id)));
        return "created";
    }

    private async Task CancelDeliveriesAsync(
        Guid deviceId,
        string reason,
        CancellationToken cancellationToken)
    {
        var deliveries = await dbContext.NotificationDeliveries
            .Where(delivery =>
                delivery.DeviceId == deviceId &&
                (delivery.Status == NotificationDeliveryStatus.Pending ||
                 delivery.Status == NotificationDeliveryStatus.Ticketed))
            .ToArrayAsync(cancellationToken);
        foreach (var delivery in deliveries)
        {
            delivery.Cancel(reason);
        }
    }
}
