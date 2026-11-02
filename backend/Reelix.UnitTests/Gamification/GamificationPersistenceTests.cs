using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Reelix.Application.Authentication;
using Reelix.Application.Gamification;
using Reelix.Domain.Catalog;
using Reelix.Domain.Gamification;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence;

namespace Reelix.UnitTests.Gamification;

public sealed class GamificationPersistenceTests
{
    [Fact]
    public async Task Service_ResolvesAuthenticatedIdentityForAllPersonalOperations()
    {
        var userId = Guid.NewGuid();
        var accounts = new StubAccounts(UserAccount.Create(userId));
        var repository = new IdentityCheckingRepository(accounts.Account.Id);
        var service = new GamificationService(repository, accounts);

        Assert.NotEqual(userId, accounts.Account.Id);
        await service.GetDashboardAsync(userId, CancellationToken.None);
        await service.UpdatePreferencesAsync(userId, true, false, CancellationToken.None);
        await service.GetStatisticsAsync(userId, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, CancellationToken.None);
        await service.GetMonthlyRankingAsync(userId, 2026, 11, null, 20, CancellationToken.None);
        Assert.Equal(4, repository.PersonalCalls);
    }

    [Fact]
    public async Task Service_PublicBadgesPreserveTargetAccountId()
    {
        var targetId = Guid.NewGuid();
        var repository = new IdentityCheckingRepository(targetId);
        var service = new GamificationService(repository, new StubAccounts(UserAccount.Create(Guid.NewGuid())));
        await service.GetPublicBadgesAsync(targetId, CancellationToken.None);
        Assert.Equal(0, repository.PersonalCalls);
    }

    private sealed class StubAccounts(UserAccount account) : IUserAccountRepository
    {
        public UserAccount Account { get; } = account;
        public Task<UserAccount> GetOrCreateAsync(Guid supabaseUserId, CancellationToken cancellationToken)
        {
            Assert.Equal(Account.SupabaseUserId, supabaseUserId);
            return Task.FromResult(Account);
        }
    }

    private sealed class IdentityCheckingRepository(Guid expectedId) : IGamificationRepository
    {
        public int PersonalCalls { get; private set; }
        private void Check(Guid accountId)
        {
            Assert.Equal(expectedId, accountId);
            PersonalCalls++;
        }
        public Task<GamificationDashboard> GetDashboardAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Check(accountId);
            return Task.FromResult<GamificationDashboard>(null!);
        }
        public Task<GamificationPreferencesData> UpdatePreferencesAsync(Guid accountId, bool badgesPublic, bool rankingEnabled, CancellationToken cancellationToken)
        {
            Check(accountId);
            return Task.FromResult<GamificationPreferencesData>(null!);
        }
        public Task<GamificationStatistics> GetStatisticsAsync(Guid accountId, DateTimeOffset periodStart, DateTimeOffset periodEnd, CancellationToken cancellationToken)
        {
            Check(accountId);
            return Task.FromResult<GamificationStatistics>(null!);
        }
        public Task<GamificationRankingPage> GetMonthlyRankingAsync(Guid accountId, DateTimeOffset periodStart, DateTimeOffset periodEnd, GamificationRankingCursor? cursor, int limit, CancellationToken cancellationToken)
        {
            Check(accountId);
            return Task.FromResult<GamificationRankingPage>(null!);
        }
        public Task<PublicBadgesData> GetPublicBadgesAsync(Guid accountId, CancellationToken cancellationToken)
        {
            Assert.Equal(expectedId, accountId);
            return Task.FromResult(new PublicBadgesData(true, []));
        }
    }

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
