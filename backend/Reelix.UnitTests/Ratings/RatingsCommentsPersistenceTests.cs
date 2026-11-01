using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Domain.Catalog;
using Reelix.Domain.Ratings;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Ratings;

public sealed class RatingsCommentsPersistenceTests
{
    [Fact]
    public void RatingsAndComments_EnforceTargetUniquenessAndPrivateReferences()
    {
        using var context = new ReelixDbContext(
            new DbContextOptionsBuilder<ReelixDbContext>()
                .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;******")
                .Options);
        var model = context.GetService<IDesignTimeModel>().Model;
        var rating = model.FindEntityType(typeof(UserRating));
        var comment = model.FindEntityType(typeof(UserComment));
        var episodeSeries = model.FindEntityType(typeof(CatalogEpisodeSeries));

        Assert.NotNull(rating);
        Assert.Equal("user_ratings", rating.GetTableName());
        Assert.True(Assert.Single(rating.GetIndexes(), index =>
            index.GetDatabaseName() == "UX_user_ratings_account_target").IsUnique);
        Assert.Contains(
            rating.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Any(property => property.Name == "TargetId") &&
                foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(
            "score * 2",
            rating.GetCheckConstraints()
                .Single(constraint => constraint.Name == "CK_user_ratings_score").Sql,
            StringComparison.Ordinal);

        Assert.NotNull(comment);
        Assert.Equal("user_comments", comment.GetTableName());
        Assert.Contains(comment.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_user_comments_target_visibility_created");
        Assert.Contains(comment.GetIndexes(), index =>
            index.GetDatabaseName() == "IX_user_comments_moderation_queue");
        Assert.Contains(
            comment.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Any(property => property.Name == "AccountId") &&
                foreignKey.DeleteBehavior == DeleteBehavior.Cascade);

        Assert.NotNull(episodeSeries);
        Assert.Equal("catalog_episode_series", episodeSeries.GetTableName());
        Assert.Equal(
            nameof(CatalogEpisodeSeries.EpisodeId),
            Assert.Single(episodeSeries.FindPrimaryKey()!.Properties).Name);
    }

    [Fact]
    public void Migration_CreatesRatingsCommentsAndEpisodeLinksWithRls()
    {
        using var context = new ReelixDbContext(
            new DbContextOptionsBuilder<ReelixDbContext>()
                .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;******")
                .Options);
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        const string migrationId = "20261101183022_AddRatingsComments";
        var migration = migrationsAssembly.CreateMigration(
            migrationsAssembly.Migrations[migrationId],
            context.Database.ProviderName!);

        var tables = migration.UpOperations
            .OfType<CreateTableOperation>()
            .Select(operation => operation.Name)
            .ToHashSet(StringComparer.Ordinal);
        Assert.Contains("user_ratings", tables);
        Assert.Contains("user_comments", tables);
        Assert.Contains("catalog_episode_series", tables);

        var securitySql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("ENABLE ROW LEVEL SECURITY", securitySql);
        Assert.Contains("REVOKE ALL", securitySql);
        Assert.Contains("user_comments", securitySql);
        Assert.Contains("user_ratings", securitySql);
    }
}
