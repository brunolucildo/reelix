using Microsoft.EntityFrameworkCore;
using Reelix.Infrastructure.Persistence;
using Reelix.Infrastructure.Persistence.Users;
using Reelix.UnitTests.Common;
using Testcontainers.PostgreSql;

namespace Reelix.UnitTests.Api;

/// <summary>
/// The account port against a real PostgreSQL (spec 0013, R3; needs Docker, like the other
/// Testcontainers tests): <c>Find</c> never writes, <c>Ensure</c> provisions and concurrent
/// <c>Ensure</c> calls end up with one account.
/// </summary>
public sealed class UserAccountRepositoryPostgresTests(AccountsDatabaseFixture database)
    : IClassFixture<AccountsDatabaseFixture>
{
    [Fact]
    public async Task Find_ReturnsNullAndCreatesNoRowForAnUnknownSubject()
    {
        var subject = Guid.NewGuid();
        var rowsBefore = await database.CountAccountsAsync();

        await using var context = database.CreateContext();
        var found = await new UserAccountRepository(context, FixedTimeProvider.AtDefault())
            .FindAsync(subject, CancellationToken.None);

        Assert.Null(found);
        Assert.Equal(rowsBefore, await database.CountAccountsAsync());
        Assert.Equal(0, await database.CountAccountsAsync(subject));
    }

    [Fact]
    public async Task Ensure_CreatesTheAccountWithTheInjectedInstantAndFindReturnsIt()
    {
        var subject = Guid.NewGuid();
        var clock = FixedTimeProvider.AtDefault();

        await using var context = database.CreateContext();
        var repository = new UserAccountRepository(context, clock);
        var created = await repository.EnsureAsync(subject, CancellationToken.None);
        var ensuredAgain = await repository.EnsureAsync(subject, CancellationToken.None);
        var found = await repository.FindAsync(subject, CancellationToken.None);

        Assert.Equal(subject, created.SupabaseUserId);
        Assert.Equal(FixedTimeProvider.DefaultNow, created.CreatedAt);
        Assert.Equal(created.Id, ensuredAgain.Id);
        Assert.Equal(created.Id, found?.Id);
        Assert.Equal(1, await database.CountAccountsAsync(subject));
    }

    [Fact]
    public async Task Ensure_EightConcurrentCallsCreateASingleAccountWithTheSameId()
    {
        var subject = Guid.NewGuid();
        using var start = new ManualResetEventSlim(false);

        var tasks = Enumerable.Range(0, 8).Select(_ => Task.Run(async () =>
        {
            await using var context = database.CreateContext();
            var repository = new UserAccountRepository(context, TimeProvider.System);
            start.Wait();
            return (await repository.EnsureAsync(subject, CancellationToken.None)).Id;
        })).ToArray();
        start.Set();
        var ids = await Task.WhenAll(tasks);

        Assert.Single(ids.Distinct());
        Assert.Equal(1, await database.CountAccountsAsync(subject));
    }
}

public sealed class AccountsDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("reelix_accounts")
        .WithUsername("reelix_test")
        .WithPassword("reelix_test")
        .Build();

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync() => await _database.DisposeAsync();

    public ReelixDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReelixDbContext>()
            .UseNpgsql(_database.GetConnectionString())
            .Options);

    public async Task<int> CountAccountsAsync(Guid? supabaseUserId = null)
    {
        await using var context = CreateContext();
        return supabaseUserId is { } subject
            ? await context.UserAccounts.CountAsync(account => account.SupabaseUserId == subject)
            : await context.UserAccounts.CountAsync();
    }
}
