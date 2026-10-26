using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Catalog;

public sealed class CatalogMigrationTests
{
    [Fact]
    public void CatalogMigration_IsDiscoverableAndCreatesUniqueExternalReferences()
    {
        var options = new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .Options;
        using var context = new ReelixDbContext(options);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        const string migrationId = "20261026193000_AddExternalCatalogReferences";

        Assert.Contains(migrationId, migrationsAssembly.Migrations.Keys);
        var migration = migrationsAssembly.CreateMigration(
            migrationsAssembly.Migrations[migrationId],
            context.Database.ProviderName!);

        var table = Assert.Single(
            migration.UpOperations.OfType<CreateTableOperation>(),
            operation => operation.Name == "catalog_external_references");
        var uniqueIndex = Assert.Single(
            migration.UpOperations.OfType<CreateIndexOperation>(),
            operation => operation.Name == "UX_catalog_external_references_external_key");
        Assert.True(uniqueIndex.IsUnique);
        Assert.Equal(["provider", "external_id", "content_type"], uniqueIndex.Columns);
    }
}
