using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Domain.Social;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Social;

public sealed class SocialPersistenceModelTests
{
    [Fact]
    public void SocialModel_EnforcesCanonicalUniqueRelationshipsAndPrivacyDefaults()
    {
        using var context = CreateContext();
        var model = context.GetService<IDesignTimeModel>().Model;
        var relationship = model.FindEntityType(typeof(FriendRelationship));
        var profile = model.FindEntityType(typeof(UserProfile));

        Assert.NotNull(relationship);
        Assert.Equal("friend_relationships", relationship.GetTableName());
        Assert.True(Assert.Single(relationship.GetIndexes(), index =>
            index.GetDatabaseName() == "UX_friend_relationships_pair").IsUnique);
        Assert.Contains(
            relationship.GetCheckConstraints(),
            constraint => constraint.Name == "CK_friend_relationships_participants");
        Assert.Contains(
            relationship.GetForeignKeys(),
            foreignKey => foreignKey.DeleteBehavior == DeleteBehavior.Cascade);

        Assert.NotNull(profile);
        Assert.Equal("user_profiles", profile.GetTableName());
        Assert.Contains(
            profile.GetCheckConstraints(),
            constraint => constraint.Name == "CK_user_profiles_friend_history_consent");
        Assert.Contains(
            profile.GetCheckConstraints(),
            constraint => constraint.Name == "CK_user_profiles_friend_ratings_consent");
    }

    [Fact]
    public void AddSocialMigration_CreatesRelationshipAndEventTables()
    {
        using var context = CreateContext();
        var migrationsAssembly = context.GetService<IMigrationsAssembly>();
        var migration = migrationsAssembly.CreateMigration(
            migrationsAssembly.Migrations["20261101190016_AddSocial"],
            context.Database.ProviderName!);
        var createdTables = migration.UpOperations
            .OfType<CreateTableOperation>()
            .Select(operation => operation.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("friend_relationships", createdTables);
        Assert.Contains("social_events", createdTables);
        Assert.Contains(
            migration.UpOperations.OfType<AddColumnOperation>(),
            operation => operation.Name == "watch_history_shared_with_friends_since");
        var securitySql = Assert.Single(migration.UpOperations.OfType<SqlOperation>()).Sql;
        Assert.Contains("ENABLE ROW LEVEL SECURITY", securitySql);
        Assert.Contains("REVOKE ALL", securitySql);
        Assert.Contains("social_events", securitySql);
    }

    [Fact]
    public void SocialQueries_TranslateCursorComparisonsAndCaseInsensitiveDiscovery()
    {
        using var context = CreateContext();
        var cursorId = Guid.NewGuid();
        var cursorQuery = context.FriendRelationships
            .Where(relationship => relationship.Id.CompareTo(cursorId) < 0);
        var discoveryQuery = context.UserProfiles
            .Where(profile => EF.Functions.ILike(profile.DisplayName!, "name%", "\\"));

        Assert.Contains("<", cursorQuery.ToQueryString(), StringComparison.Ordinal);
        Assert.Contains("ILIKE", discoveryQuery.ToQueryString(), StringComparison.Ordinal);
    }

    private static ReelixDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .Options);
}
