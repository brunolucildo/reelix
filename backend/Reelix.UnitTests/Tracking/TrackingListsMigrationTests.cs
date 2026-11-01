using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Tracking;

public sealed class TrackingListsMigrationTests
{
    [Fact]
    public void Migration_CreatesFeatureTablesUniqueKeysAndRestrictsDatabaseRoles()
    {
        var options = new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .Options;
        using var context = new ReelixDbContext(options);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        const string migrationId = "20261101141707_AddTrackingAndLists";
        var migration = migrationsAssembly.CreateMigration(
            migrationsAssembly.Migrations[migrationId],
            context.Database.ProviderName!);

        var createdTables = migration.UpOperations
            .OfType<CreateTableOperation>()
            .Select(operation => operation.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("tracking_records", createdTables);
        Assert.Contains("user_watchlist_titles", createdTables);
        Assert.Contains("user_custom_lists", createdTables);
        Assert.Contains("user_custom_list_titles", createdTables);

        var uniqueTrackingIndex = Assert.Single(
            migration.UpOperations.OfType<CreateIndexOperation>(),
            operation => operation.Name == "UX_tracking_records_account_target");
        Assert.True(uniqueTrackingIndex.IsUnique);

        var securitySql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("ENABLE ROW LEVEL SECURITY", securitySql);
        Assert.Contains("REVOKE ALL", securitySql);
    }
}
