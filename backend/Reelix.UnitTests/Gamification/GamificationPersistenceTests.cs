using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Domain.Catalog;
using Reelix.Domain.Gamification;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Gamification;

public sealed class GamificationPersistenceTests
{
    [Fact]
    public void Model_MapsUniqueAchievementsAndVersionedCatalogMetadata()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var achievement = model.FindEntityType(typeof(UserAchievement));
        var preferences = model.FindEntityType(typeof(GamificationPreferences));
        var definition = model.FindEntityType(typeof(AchievementDefinition));
        var catalogReference = model.FindEntityType(typeof(ExternalCatalogReference));

        Assert.NotNull(achievement);
        Assert.Equal("user_achievements", achievement.GetTableName());
        Assert.Equal(
            ["AccountId", "DefinitionId"],
            achievement.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.NotNull(preferences);
        Assert.Equal("gamification_preferences", preferences.GetTableName());
        Assert.NotNull(definition);
        Assert.Equal("achievement_definitions", definition.GetTableName());
        Assert.Equal(
            5,
            definition.GetSeedData().Count());
        Assert.NotNull(catalogReference);
        Assert.Equal(
            "runtime_minutes",
            catalogReference.FindProperty(nameof(ExternalCatalogReference.RuntimeMinutes))!
                .GetColumnName());
        Assert.Equal(
            "metadata_version",
            catalogReference.FindProperty(nameof(ExternalCatalogReference.MetadataVersion))!
                .GetColumnName());
    }

    [Fact]
    public void Migration_CreatesGamificationTablesAndAppliesPrivateTablePolicies()
    {
        using var context = CreateContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var migration = migrationsAssembly.CreateMigration(
            migrationsAssembly.Migrations["20261101193618_AddGamification"],
            context.Database.ProviderName!);
        var createdTables = migration.UpOperations
            .OfType<CreateTableOperation>()
            .Select(operation => operation.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("achievement_definitions", createdTables);
        Assert.Contains("user_achievements", createdTables);
        Assert.Contains("gamification_preferences", createdTables);
        Assert.Contains("catalog_reference_genres", createdTables);
        Assert.Contains(
            migration.UpOperations.OfType<AddColumnOperation>(),
            operation => operation.Name == "runtime_minutes");
        var securitySql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("ENABLE ROW LEVEL SECURITY", securitySql);
        Assert.Contains("REVOKE ALL", securitySql);
        Assert.Contains("gamification_preferences", securitySql);
        Assert.Contains("user_achievements", securitySql);
    }

    private static ReelixDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .Options);
}
