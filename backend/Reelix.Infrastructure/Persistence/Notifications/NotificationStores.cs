using Microsoft.EntityFrameworkCore;
using Reelix.Application.Notifications;
using Reelix.Domain.Notifications;
using Reelix.Domain.Social;

namespace Reelix.Infrastructure.Persistence.Notifications;

public sealed class NotificationPreferencesStore(ReelixDbContext dbContext) : INotificationPreferencesStore
{
    public Task<NotificationPreferences?> FindAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.NotificationPreferences
            .SingleOrDefaultAsync(item => item.AccountId == accountId, cancellationToken);

    public void Add(NotificationPreferences preferences) =>
        dbContext.NotificationPreferences.Add(preferences);
}

public sealed class NotificationDeviceRegistry(ReelixDbContext dbContext) : INotificationDevices
{
    public Task<NotificationDevice?> FindByInstallationAsync(
        Guid installationId,
        CancellationToken cancellationToken) =>
        dbContext.NotificationDevices.SingleOrDefaultAsync(
            item => item.InstallationId == installationId,
            cancellationToken);

    public Task<bool> TokenInUseAsync(
        string expoPushToken,
        Guid? exceptDeviceId,
        CancellationToken cancellationToken) =>
        dbContext.NotificationDevices.AnyAsync(
            item => item.ExpoPushToken == expoPushToken &&
                item.IsActive &&
                (exceptDeviceId == null || item.Id != exceptDeviceId),
            cancellationToken);

    public Task<NotificationDevice?> FindActiveOwnedAsync(
        Guid accountId,
        Guid deviceId,
        CancellationToken cancellationToken) =>
        dbContext.NotificationDevices.SingleOrDefaultAsync(
            item => item.Id == deviceId && item.AccountId == accountId && item.IsActive,
            cancellationToken);

    public void Add(NotificationDevice device) => dbContext.NotificationDevices.Add(device);
}

public sealed class NotificationDeviceLookup(ReelixDbContext dbContext) : INotificationDeviceLookup
{
    public async Task<IReadOnlyList<NotificationDevice>> ListActiveAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.NotificationDevices
            .Where(device => device.AccountId == accountId && device.IsActive)
            .ToArrayAsync(cancellationToken);

    public Task<NotificationDevice?> FindAsync(Guid deviceId, CancellationToken cancellationToken) =>
        dbContext.NotificationDevices.SingleOrDefaultAsync(item => item.Id == deviceId, cancellationToken);
}

public sealed class NotificationDeliveryStore(ReelixDbContext dbContext) : INotificationDeliveries
{
    public async Task<IReadOnlyList<NotificationDelivery>> ListPendingForCategoriesAsync(
        Guid accountId,
        IReadOnlyCollection<NotificationCategory> categories,
        CancellationToken cancellationToken)
    {
        if (categories.Count == 0)
        {
            return [];
        }

        var categoryList = categories.ToArray();
        return await (
                from delivery in dbContext.NotificationDeliveries
                join notification in dbContext.UserNotifications
                    on delivery.NotificationId equals notification.Id
                where notification.AccountId == accountId &&
                    delivery.Status == NotificationDeliveryStatus.Pending &&
                    categoryList.Contains(notification.Category)
                select delivery)
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationDelivery>> ListOpenForDeviceAsync(
        Guid deviceId,
        CancellationToken cancellationToken) =>
        await dbContext.NotificationDeliveries
            .Where(delivery =>
                delivery.DeviceId == deviceId &&
                (delivery.Status == NotificationDeliveryStatus.Pending ||
                 delivery.Status == NotificationDeliveryStatus.Ticketed))
            .ToArrayAsync(cancellationToken);

    public Task<NotificationDelivery?> FindLeasedAsync(
        Guid deliveryId,
        Guid leaseId,
        CancellationToken cancellationToken) =>
        dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            item => item.Id == deliveryId && item.LeaseId == leaseId,
            cancellationToken);

    public Task<NotificationDelivery?> FindTicketedAsync(
        Guid deliveryId,
        string ticketId,
        CancellationToken cancellationToken) =>
        dbContext.NotificationDeliveries.SingleOrDefaultAsync(
            item => item.Id == deliveryId &&
                item.ExpoTicketId == ticketId &&
                item.Status == NotificationDeliveryStatus.Ticketed,
            cancellationToken);
}

public sealed class UserNotificationStore(ReelixDbContext dbContext) : IUserNotifications
{
    public async Task<IReadOnlyList<NotificationInboxItem>> ListPageAsync(
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

        var rows = await query
            .OrderByDescending(item => item.CreatedAt)
            .ThenByDescending(item => item.Id)
            .Take(limit)
            .Select(item => new
            {
                item.Id,
                item.Category,
                item.Title,
                item.Body,
                item.DeepLink,
                item.CreatedAt,
                item.ReadAt,
            })
            .ToArrayAsync(cancellationToken);
        return rows
            .Select(row => new NotificationInboxItem(
                row.Id,
                row.Category.ToApplication(),
                row.Title,
                row.Body,
                row.DeepLink,
                row.CreatedAt,
                row.ReadAt))
            .ToArray();
    }

    public Task<int> CountUnreadAsync(Guid accountId, CancellationToken cancellationToken) =>
        dbContext.UserNotifications.CountAsync(
            item => item.AccountId == accountId && item.ReadAt == null,
            cancellationToken);

    public Task<UserNotification?> FindOwnedAsync(
        Guid accountId,
        Guid notificationId,
        CancellationToken cancellationToken) =>
        dbContext.UserNotifications.SingleOrDefaultAsync(
            item => item.Id == notificationId && item.AccountId == accountId,
            cancellationToken);
}

/// <summary>
/// Reads the friend table, which belongs to Social, until Social exposes a port (spec 0016).
/// </summary>
public sealed class FriendshipLookup(ReelixDbContext dbContext) : IFriendships
{
    public Task<FriendRelationship?> FindBetweenAsync(
        Guid firstAccountId,
        Guid secondAccountId,
        CancellationToken cancellationToken) =>
        dbContext.FriendRelationships
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.FirstAccountId == firstAccountId && item.SecondAccountId == secondAccountId ||
                    item.FirstAccountId == secondAccountId && item.SecondAccountId == firstAccountId,
                cancellationToken);
}

public sealed class NotificationsUnitOfWork(ReelixDbContext dbContext) : INotificationsUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
