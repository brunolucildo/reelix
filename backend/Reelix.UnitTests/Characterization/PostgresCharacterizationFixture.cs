using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using Reelix.Domain.Catalog;
using Reelix.Domain.Social;
using Reelix.Domain.Tracking;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence;
using Reelix.UnitTests.Api;
using Reelix.UnitTests.Common;
using Testcontainers.PostgreSql;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// One PostgreSQL container shared by the characterization tests of Gamification and
/// Notifications (spec 0014, Batch 0). The tests of the collection run one after the other, so
/// the shared clock and the shared fake Expo server can be moved by each test.
/// </summary>
[CollectionDefinition(Name)]
public sealed class PostgresCharacterizationCollection : ICollectionFixture<PostgresCharacterizationFixture>
{
    public const string Name = "postgres-characterization";
}

public sealed record TestAccount(Guid Subject, Guid AccountId);

public sealed class PostgresCharacterizationFixture : IAsyncLifetime
{
    public static readonly Guid ExpoProjectId = Guid.Parse("11111111-2222-3333-4444-555555555555");

    private readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:17-alpine")
        .WithDatabase("reelix_characterization")
        .WithUsername("reelix_test")
        .WithPassword("reelix_test")
        .Build();

    public string ConnectionString => _database.GetConnectionString();

    /// <summary>The clock of both hosts; a test sets the instant it needs.</summary>
    public FixedTimeProvider Clock { get; } = FixedTimeProvider.AtDefault();

    public FakeExpoHandler Expo { get; } = new();

    /// <summary>Every SQL command the hosts send, so a test can prove a read wrote nothing.</summary>
    public CommandLog Commands { get; } = new();

    /// <summary>Host with the Expo push feature enabled (registration and delivery).</summary>
    public CharacterizationApiFactory PushEnabled { get; private set; } = null!;

    /// <summary>Host with the Expo push feature disabled (the default).</summary>
    public CharacterizationApiFactory PushDisabled { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _database.StartAsync();
        await using (var dbContext = CreateDbContext())
        {
            await dbContext.Database.MigrateAsync();
        }

        PushEnabled = new CharacterizationApiFactory(this, pushEnabled: true);
        PushDisabled = new CharacterizationApiFactory(this, pushEnabled: false);
    }

    public async Task DisposeAsync()
    {
        PushEnabled?.Dispose();
        PushDisabled?.Dispose();
        await _database.DisposeAsync();
    }

    public ReelixDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<ReelixDbContext>().UseNpgsql(ConnectionString).Options);

    /// <summary>Creates an account row directly (no HTTP call) for a new Supabase subject.</summary>
    public async Task<TestAccount> CreateAccountAsync()
    {
        var subject = Guid.NewGuid();
        await using var dbContext = CreateDbContext();
        var account = UserAccount.Create(subject, Clock.GetUtcNow());
        dbContext.UserAccounts.Add(account);
        await dbContext.SaveChangesAsync();
        return new TestAccount(subject, account.Id);
    }

    public async Task SetDisplayNameAsync(Guid accountId, string displayName)
    {
        await using var dbContext = CreateDbContext();
        var profile = UserProfile.Create(accountId);
        profile.Update(displayName, null, false, false, false, Clock.GetUtcNow());
        dbContext.UserProfiles.Add(profile);
        await dbContext.SaveChangesAsync();
    }

    /// <summary>Marks <paramref name="count"/> distinct movies as watched, one genre each when asked.</summary>
    public async Task<IReadOnlyList<Guid>> SeedMoviesAsync(
        Guid accountId,
        int count,
        int? firstGenreId = null,
        DateTimeOffset? watchedAt = null,
        int? runtimeMinutes = null,
        string provider = "tmdb")
    {
        await using var dbContext = CreateDbContext();
        var now = Clock.GetUtcNow();
        var ids = new List<Guid>();
        for (var index = 0; index < count; index++)
        {
            var reference = ExternalCatalogReference.Create(provider, Guid.NewGuid().ToString("N"), "movie", now);
            if (runtimeMinutes is not null)
            {
                reference.UpdateMetadata(runtimeMinutes, hasGenreIds: false, hasChanged: true, now);
            }

            dbContext.ExternalCatalogReferences.Add(reference);
            await dbContext.SaveChangesAsync();
            if (firstGenreId is { } genreStart)
            {
                dbContext.CatalogReferenceGenres.Add(
                    CatalogReferenceGenre.Create(reference.Id, genreStart + index));
            }

            dbContext.TrackingRecords.Add(
                TrackingRecord.CreateMovie(accountId, reference.Id, now, watchedAt ?? now));
            ids.Add(reference.Id);
        }

        await dbContext.SaveChangesAsync();
        return ids;
    }

    /// <summary>Marks episodes as watched: <paramref name="episodesPerSeries"/> in each of <paramref name="seriesCount"/> series.</summary>
    public async Task SeedEpisodesAsync(Guid accountId, int seriesCount, int episodesPerSeries)
    {
        await using var dbContext = CreateDbContext();
        var now = Clock.GetUtcNow();
        for (var seriesIndex = 0; seriesIndex < seriesCount; seriesIndex++)
        {
            var series = ExternalCatalogReference.Create("tmdb", Guid.NewGuid().ToString("N"), "series", now);
            dbContext.ExternalCatalogReferences.Add(series);
            await dbContext.SaveChangesAsync();
            for (var episodeIndex = 0; episodeIndex < episodesPerSeries; episodeIndex++)
            {
                var episode = ExternalCatalogReference.Create("tmdb", Guid.NewGuid().ToString("N"), "episode", now);
                dbContext.ExternalCatalogReferences.Add(episode);
                await dbContext.SaveChangesAsync();
                dbContext.TrackingRecords.Add(
                    TrackingRecord.CreateEpisode(accountId, series.Id, episode.Id, now, now));
            }
        }

        await dbContext.SaveChangesAsync();
    }

    public async Task RemoveOneMovieAsync(Guid accountId)
    {
        await using var dbContext = CreateDbContext();
        var record = await dbContext.TrackingRecords.FirstAsync(
            item => item.AccountId == accountId && item.ItemType == TrackingItemType.Movie);
        dbContext.TrackingRecords.Remove(record);
        await dbContext.SaveChangesAsync();
    }

    public async Task MakeFriendsAsync(Guid firstAccountId, Guid secondAccountId)
    {
        await using var dbContext = CreateDbContext();
        var relationship = FriendRelationship.Create(firstAccountId, secondAccountId, Clock.GetUtcNow());
        relationship.Accept(secondAccountId, Clock.GetUtcNow());
        dbContext.FriendRelationships.Add(relationship);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> CountAsync(string table, string accountColumn, Guid accountId)
    {
        await using var dbContext = CreateDbContext();
        await using var command = dbContext.Database.GetDbConnection().CreateCommand();
        await dbContext.Database.OpenConnectionAsync();
        command.CommandText = $"SELECT count(*) FROM public.{table} WHERE {accountColumn} = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "id";
        parameter.Value = accountId;
        command.Parameters.Add(parameter);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task ExecuteSqlAsync(string sql)
    {
        await using var dbContext = CreateDbContext();
        await dbContext.Database.ExecuteSqlRawAsync(sql);
    }
}

/// <summary>An in-memory stand-in for the Expo Push API, driven by the test.</summary>
public sealed class FakeExpoHandler : HttpMessageHandler
{
    private readonly object _gate = new();
    private readonly List<(string Path, string Body)> _requests = [];

    public Func<HttpRequestMessage, string, HttpResponseMessage> Responder { get; set; } =
        (_, _) => new HttpResponseMessage(System.Net.HttpStatusCode.InternalServerError);

    public IReadOnlyList<(string Path, string Body)> Requests
    {
        get
        {
            lock (_gate)
            {
                return _requests.ToArray();
            }
        }
    }

    public void Reset()
    {
        lock (_gate)
        {
            _requests.Clear();
        }
    }

    public static HttpResponseMessage Json(string json, System.Net.HttpStatusCode status = System.Net.HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? string.Empty
            : await request.Content.ReadAsStringAsync(cancellationToken);
        lock (_gate)
        {
            _requests.Add((request.RequestUri!.AbsolutePath, body));
        }

        return Responder(request, body);
    }
}

public sealed class CharacterizationApiFactory : WebApplicationFactory<Program>
{
    private readonly RSA _signingKey = RSA.Create(2048);
    private readonly PostgresCharacterizationFixture _fixture;
    private readonly bool _pushEnabled;
    private bool _environmentReleased;

    public CharacterizationApiFactory(PostgresCharacterizationFixture fixture, bool pushEnabled)
    {
        _fixture = fixture;
        _pushEnabled = pushEnabled;
        ApiTestEnvironment.Acquire();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Supabase:Url"] = "https://reelix-test.supabase.co",
                ["Supabase:JwtSecret"] = string.Empty,
                ["ConnectionStrings:Reelix"] = _fixture.ConnectionString,
                ["ExpoPush:Enabled"] = _pushEnabled ? "true" : "false",
                ["ExpoPush:ProjectId"] = PostgresCharacterizationFixture.ExpoProjectId.ToString(),
                ["ExpoPush:AccessToken"] = "test-access-token",
                ["ExpoPush:MaxAttempts"] = "5",
            }));
        builder.ConfigureTestServices(services =>
        {
            // The workers are driven by the tests, one cycle at a time, never by the clock.
            services.RemoveAll<IHostedService>();
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(_fixture.Clock);
            services.RemoveAll<DbContextOptions<ReelixDbContext>>();
            services.RemoveAll<ReelixDbContext>();
            services.AddDbContext<ReelixDbContext>(options => options
                .UseNpgsql(_fixture.ConnectionString)
                .AddInterceptors(_fixture.Commands));
            services.ConfigureAll<HttpClientFactoryOptions>(options =>
                options.HttpMessageHandlerBuilderActions.Add(
                    handler => handler.PrimaryHandler = _fixture.Expo));
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = null;
                    options.ConfigurationManager = null;
                    options.TokenValidationParameters.ValidIssuer =
                        "https://reelix-test.supabase.co/auth/v1";
                    options.TokenValidationParameters.ValidAudience = "authenticated";
                    options.TokenValidationParameters.IssuerSigningKey = new RsaSecurityKey(_signingKey);
                });
        });
    }

    public string CreateToken(Guid subject)
    {
        var credentials = new SigningCredentials(
            new RsaSecurityKey(_signingKey),
            SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            "https://reelix-test.supabase.co/auth/v1",
            "authenticated",
            [new Claim("sub", subject.ToString())],
            DateTime.UtcNow.AddMinutes(-1),
            DateTime.UtcNow.AddMinutes(5),
            credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public HttpRequestMessage Request(HttpMethod method, string path, Guid subject, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", CreateToken(subject));
        if (body is not null)
        {
            request.Content = System.Net.Http.Json.JsonContent.Create(body);
        }

        return request;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _signingKey.Dispose();
            if (!_environmentReleased)
            {
                _environmentReleased = true;
                ApiTestEnvironment.Release();
            }
        }
    }
}

/// <summary>Remembers the text of every command the host sends to the database.</summary>
public sealed class CommandLog : DbCommandInterceptor
{
    private readonly object _gate = new();
    private readonly List<string> _commands = [];

    public void Clear()
    {
        lock (_gate)
        {
            _commands.Clear();
        }
    }

    /// <summary>The commands that change data (INSERT, UPDATE or DELETE).</summary>
    public IReadOnlyList<string> Writes()
    {
        lock (_gate)
        {
            return _commands
                .Where(text => text.TrimStart().StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                    text.TrimStart().StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                    text.TrimStart().StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
    }

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }

    public override InterceptionResult<int> NonQueryExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result)
    {
        Record(command);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Record(command);
        return ValueTask.FromResult(result);
    }

    private void Record(DbCommand command)
    {
        lock (_gate)
        {
            _commands.Add(command.CommandText);
        }
    }
}
