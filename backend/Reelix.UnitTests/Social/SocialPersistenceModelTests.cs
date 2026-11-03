using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Domain.Social;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence;
using Reelix.Infrastructure.Persistence.Social;

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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ActivityQueries_RequireFriendshipConsentAndPublicRatings(bool filterAuthor)
    {
        var commands = new CaptureCommandsInterceptor();
        using var context = new ReelixDbContext(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .AddInterceptors(new SuppressConnectionInterceptor(), commands)
            .Options);
        var repository = new SocialRepository(context);

        var result = await repository.GetActivityPageAsync(
            Guid.NewGuid(), null, 20, CancellationToken.None, filterAuthor ? Guid.NewGuid() : null);

        Assert.Empty(result);
        var query = commands.Commands[0];
        Assert.Contains("'Accepted'", query);
        Assert.Contains("watch_history_shared_with_friends", query);
        Assert.Contains("ratings_shared_with_friends", query);
        Assert.Contains("ratings_shared_with_friends_since", query);
        Assert.Contains("audience = 'public'", query);
        if (filterAuthor) Assert.Contains("actorAccountId", query);
    }

    [Fact]
    public async Task ProfileQueries_RequirePublicDiscoveryOrAnActiveRelationship()
    {
        var commands = new CaptureCommandsInterceptor();
        using var context = new ReelixDbContext(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .AddInterceptors(new SuppressConnectionInterceptor(), commands)
            .Options);
        var repository = new SocialRepository(context);

        var result = await repository.GetFriendProfileAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.Null(result);
        Assert.Equal(2, commands.Commands.Count);
        var query = commands.Commands[1];
        Assert.Contains("profile_is_public", query);
        Assert.Contains("is_discoverable", query);
        Assert.Contains("'Accepted'", query);
        Assert.Contains("'Pending'", query);
        Assert.Contains("first_account_id", query);
        Assert.Contains("second_account_id", query);
    }

    [Fact]
    public void FixedAvatarMigration_AddsOnlyAnOptionalValidatedChoice()
    {
        using var context = CreateContext();
        var migrations = context.GetService<IMigrationsAssembly>();
        var migration = migrations.CreateMigration(migrations.Migrations["20261103133240_AddFixedAvatars"], context.Database.ProviderName!);
        var column = Assert.Single(migration.UpOperations.OfType<AddColumnOperation>());
        Assert.Equal("user_profiles", column.Table);
        Assert.Equal("avatar_key", column.Name);
        Assert.True(column.IsNullable);
        Assert.Equal(24, column.MaxLength);
        Assert.Single(migration.UpOperations.OfType<AddCheckConstraintOperation>());
        Assert.Empty(migration.UpOperations.OfType<DropTableOperation>());
        Assert.Empty(migration.UpOperations.OfType<SqlOperation>());
    }

    private sealed class SuppressConnectionInterceptor : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(
            DbConnection connection, ConnectionEventData eventData, InterceptionResult result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult(InterceptionResult.Suppress());
    }

    private sealed class CaptureCommandsInterceptor : DbCommandInterceptor
    {
        public List<string> Commands { get; } = [];

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(InterceptionResult<DbDataReader>.SuppressWithResult(new DataTable().CreateDataReader()));
        }
    }

    private static ReelixDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql("Host=localhost;Database=reelix_test;Username=test;Password=test")
            .Options);
}
