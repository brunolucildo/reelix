using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Domain.Notifications;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Notifications;

public sealed class NotificationsPersistenceTests
{
    [Fact]
    public void NotificationsModel_UsesPrivateTablesAndIdempotencyIndexes()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        var preferences = model.FindEntityType(typeof(NotificationPreferences));
        Assert.NotNull(preferences);
        Assert.Equal("notification_preferences", preferences.GetTableName());
        Assert.Equal(
            "UX_notification_devices_installation",
            Assert.Single(model.FindEntityType(typeof(NotificationDevice))!.GetIndexes(),
                index => index.GetDatabaseName() == "UX_notification_devices_installation")
                .GetDatabaseName());

        var notifications = model.FindEntityType(typeof(UserNotification));
        Assert.NotNull(notifications);
        Assert.True(Assert.Single(notifications.GetIndexes(),
            index => index.GetDatabaseName() == "UX_user_notifications_account_source").IsUnique);
        var deliveries = model.FindEntityType(typeof(NotificationDelivery));
        Assert.NotNull(deliveries);
        Assert.Equal("notification_deliveries", deliveries.GetTableName());
        Assert.Contains(
            deliveries.GetProperties(),
            property => property.GetColumnName() == "expo_ticket_created_at");
        Assert.Equal(
            "notification_processed_social_events",
            model.FindEntityType(typeof(ProcessedNotificationEvent))!.GetTableName());
    }

    [Fact]
    public void AddNotificationsMigration_CreatesTablesAndRestrictsDatabaseRoles()
    {
        using var context = CreateContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var migration = migrationsAssembly.CreateMigration(
            migrationsAssembly.Migrations["20261101191005_AddNotifications"],
            context.Database.ProviderName!);

        var createdTables = migration.UpOperations
            .OfType<CreateTableOperation>()
            .Select(operation => operation.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("notification_preferences", createdTables);
        Assert.Contains("notification_devices", createdTables);
        Assert.Contains("user_notifications", createdTables);
        Assert.Contains("notification_deliveries", createdTables);
        Assert.Contains("notification_processed_social_events", createdTables);

        var securitySql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("ENABLE ROW LEVEL SECURITY", securitySql);
        Assert.Contains("REVOKE ALL", securitySql);
        Assert.Contains("notification_deliveries", securitySql);
    }

    [Fact]
    public void InboxCursor_TranslatesCompositeTimestampAndGuidComparison()
    {
        using var context = CreateContext();
        var cursorId = Guid.NewGuid();
        var cursorDate = DateTimeOffset.UtcNow;
        var query = context.UserNotifications.Where(notification =>
            notification.CreatedAt < cursorDate ||
            notification.CreatedAt == cursorDate && notification.Id.CompareTo(cursorId) < 0);

        Assert.Contains("<", query.ToQueryString(), StringComparison.Ordinal);
    }

    private static ReelixDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .Options);
}
