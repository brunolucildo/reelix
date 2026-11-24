using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Gamification;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// Batch 0 of spec 0014 (R1): the achievement rules observed through <c>GET /gamification/dashboard</c>
/// on PostgreSQL, written against the code that still keeps the rules in the repository.
/// </summary>
[Collection(PostgresCharacterizationCollection.Name)]
public sealed class GamificationAchievementsCharacterizationTests(PostgresCharacterizationFixture fixture)
    : CharacterizationTestBase(fixture)
{
    public static TheoryData<string, string, int, bool> ThresholdCases => new()
    {
        { "movies", "first-movie", 0, false },
        { "movies", "first-movie", 1, true },
        { "movies", "first-movie", 2, true },
        { "movies", "cinephile", 9, false },
        { "movies", "cinephile", 10, true },
        { "movies", "cinephile", 11, true },
        { "episodes", "first-marathon", 9, false },
        { "episodes", "first-marathon", 10, true },
        { "episodes", "first-marathon", 11, true },
        { "series", "series-in-focus", 2, false },
        { "series", "series-in-focus", 3, true },
        { "series", "series-in-focus", 4, true },
        { "genres", "genre-explorer", 4, false },
        { "genres", "genre-explorer", 5, true },
        { "genres", "genre-explorer", 6, true },
    };

    [Theory]
    [MemberData(nameof(ThresholdCases))]
    public async Task Dashboard_GrantsAnAchievementOnlyAtTheThreshold(
        string rule,
        string code,
        int count,
        bool expectedEarned)
    {
        var account = await Fixture.CreateAccountAsync();
        switch (rule)
        {
            case "movies":
                await Fixture.SeedMoviesAsync(account.AccountId, count);
                break;
            case "episodes":
                await Fixture.SeedEpisodesAsync(account.AccountId, 1, count);
                break;
            case "series":
                await Fixture.SeedEpisodesAsync(account.AccountId, count, 1);
                break;
            case "genres":
                await Fixture.SeedMoviesAsync(account.AccountId, count, firstGenreId: 1);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(rule));
        }

        var response = await GetAsync(account.Subject, "/api/v1/gamification/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var achievement = Achievement(response.Json(), code);
        Assert.Equal(count, achievement.GetProperty("currentValue").GetInt32());
        Assert.Equal(expectedEarned, achievement.GetProperty("isEarned").GetBoolean());
        Assert.Equal(
            expectedEarned ? "earned" : null,
            achievement.GetProperty("status").ValueKind == JsonValueKind.Null
                ? null
                : achievement.GetProperty("status").GetString());
        Assert.Equal(
            expectedEarned ? Iso(Clock.GetUtcNow()) : null,
            achievement.GetProperty("firstEarnedAt").ValueKind == JsonValueKind.Null
                ? null
                : achievement.GetProperty("firstEarnedAt").GetString());
    }

    [Fact]
    public async Task Dashboard_RevokesBelowTheThresholdAndReactivatesKeepingTheFirstEarnedDate()
    {
        var account = await Fixture.CreateAccountAsync();
        var firstEarnedAt = Clock.GetUtcNow();
        await Fixture.SeedMoviesAsync(account.AccountId, 10);

        var earned = Achievement(
            (await GetAsync(account.Subject, "/api/v1/gamification/dashboard")).Json(),
            "cinephile");
        Assert.Equal("earned", earned.GetProperty("status").GetString());
        Assert.Equal(Iso(firstEarnedAt), earned.GetProperty("firstEarnedAt").GetString());
        Assert.Equal(JsonValueKind.Null, earned.GetProperty("lastRevokedAt").ValueKind);
        Assert.Equal(JsonValueKind.Null, earned.GetProperty("reactivatedAt").ValueKind);

        Clock.Advance(TimeSpan.FromHours(1));
        var revokedAt = Clock.GetUtcNow();
        await Fixture.RemoveOneMovieAsync(account.AccountId);
        var revoked = Achievement(
            (await GetAsync(account.Subject, "/api/v1/gamification/dashboard")).Json(),
            "cinephile");
        Assert.False(revoked.GetProperty("isEarned").GetBoolean());
        Assert.Equal("revoked", revoked.GetProperty("status").GetString());
        Assert.Equal(9, revoked.GetProperty("currentValue").GetInt32());
        Assert.Equal(Iso(firstEarnedAt), revoked.GetProperty("firstEarnedAt").GetString());
        Assert.Equal(Iso(revokedAt), revoked.GetProperty("lastRevokedAt").GetString());

        Clock.Advance(TimeSpan.FromHours(1));
        var reactivatedAt = Clock.GetUtcNow();
        await Fixture.SeedMoviesAsync(account.AccountId, 1);
        var reactivated = Achievement(
            (await GetAsync(account.Subject, "/api/v1/gamification/dashboard")).Json(),
            "cinephile");
        Assert.True(reactivated.GetProperty("isEarned").GetBoolean());
        Assert.Equal("earned", reactivated.GetProperty("status").GetString());
        Assert.Equal(Iso(firstEarnedAt), reactivated.GetProperty("firstEarnedAt").GetString());
        Assert.Equal(Iso(revokedAt), reactivated.GetProperty("lastRevokedAt").GetString());
        Assert.Equal(Iso(reactivatedAt), reactivated.GetProperty("reactivatedAt").GetString());
        Assert.Equal(
            1,
            await Fixture.CountAsync("user_achievements", "account_id", account.AccountId) -
                await CountOtherAchievementsAsync(account.AccountId));
    }

    [Fact]
    public async Task Dashboard_IgnoresInactiveDefinitions()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(account.AccountId, 1);
        await Fixture.ExecuteSqlAsync(
            "UPDATE public.achievement_definitions SET is_active = FALSE WHERE code = 'first-movie'");
        try
        {
            var response = await GetAsync(account.Subject, "/api/v1/gamification/dashboard");

            Assert.Equal(HttpStatusCode.OK, response.Status);
            var codes = response.Json().GetProperty("achievements").EnumerateArray()
                .Select(item => item.GetProperty("code").GetString())
                .ToArray();
            Assert.DoesNotContain("first-movie", codes);
            Assert.Equal(4, codes.Length);
            Assert.Equal(0, await Fixture.CountAsync("user_achievements", "account_id", account.AccountId));
        }
        finally
        {
            await Fixture.ExecuteSqlAsync(
                "UPDATE public.achievement_definitions SET is_active = TRUE WHERE code = 'first-movie'");
        }
    }

    [Fact]
    public async Task Dashboard_FlagsIncompleteGenreMetadataOnlyOnTheGenreAchievement()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(account.AccountId, 2, firstGenreId: 1);
        await Fixture.SeedMoviesAsync(account.AccountId, 1);

        var response = await GetAsync(account.Subject, "/api/v1/gamification/dashboard");

        var body = response.Json();
        Assert.Equal(
            1,
            body.GetProperty("lifetimeStatistics").GetProperty("moviesMissingGenreMetadata").GetInt32());
        Assert.True(Achievement(body, "genre-explorer").GetProperty("isMetadataIncomplete").GetBoolean());
        foreach (var code in new[] { "first-movie", "cinephile", "first-marathon", "series-in-focus" })
        {
            Assert.False(Achievement(body, code).GetProperty("isMetadataIncomplete").GetBoolean());
        }
    }

    [Fact]
    public async Task Dashboard_ReturnsTheCompleteJsonContract()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(account.AccountId, 3, firstGenreId: 1, runtimeMinutes: 120);
        await Fixture.SeedEpisodesAsync(account.AccountId, 2, 2);
        var now = Iso(Clock.GetUtcNow());

        var response = await GetAsync(account.Subject, "/api/v1/gamification/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Equal("application/json", response.ContentType);
        var unearned = (string code, string title, string description, int value, int threshold) =>
            $$"""
            {"code":"{{code}}","title":"{{title}}","description":"{{description}}","currentValue":{{value}},"threshold":{{threshold}},"isEarned":false,"isMetadataIncomplete":false,"status":null,"firstEarnedAt":null,"lastRevokedAt":null,"reactivatedAt":null}
            """;
        var expected = $$"""
            {
              "preferences": {"badgesPublic": true, "rankingEnabled": false, "updatedAt": "{{now}}"},
              "lifetimeStatistics": {
                "periodStart": "0001-01-01T00:00:00+00:00",
                "periodEnd": "9999-12-31T23:59:59.9999999+00:00",
                "moviesWatched": 3, "episodesWatched": 4, "seriesExplored": 2, "genresExplored": 3,
                "moviesMissingGenreMetadata": 0, "itemsWithRuntime": 3, "itemsWithoutRuntime": 4,
                "estimatedWatchMinutes": 360, "estimatedWatchHours": 6, "runtimeSources": ["tmdb"],
                "runtimeMetadataVersion": 1, "runtimeMetadataUpdatedAt": "{{now}}",
                "confirmedWatchMinutes": null, "calculatedAt": "{{now}}",
                "runtimeCoverage": 0.42857142857142855
              },
              "achievements": [
                {{unearned("cinephile", "Cinéfilo", "Registre 10 filmes distintos como assistidos.", 3, 10)}},
                {{unearned("first-marathon", "Maratona inicial", "Registre 10 episódios distintos como assistidos.", 4, 10)}},
                {"code":"first-movie","title":"Primeiro filme","description":"Registre seu primeiro filme assistido.","currentValue":3,"threshold":1,"isEarned":true,"isMetadataIncomplete":false,"status":"earned","firstEarnedAt":"{{now}}","lastRevokedAt":null,"reactivatedAt":null},
                {{unearned("genre-explorer", "Explorador de gêneros", "Assista a filmes de cinco gêneros canônicos distintos.", 3, 5)}},
                {{unearned("series-in-focus", "Séries em foco", "Assista a pelo menos um episódio em três séries distintas.", 2, 3)}}
              ]
            }
            """;
        AssertSameJson(expected, response.Body);
    }

    [Fact]
    public async Task EightConcurrentDashboards_NeverDuplicateRows_ButMayFailWithASerializationError_KnownDefect()
    {
        // FINDING of spec 0014 (Batch 0): the RepeatableRead transaction of the dashboard raises
        // PostgreSQL 40001 (could not serialize access due to concurrent update) when several
        // dashboards of the same account run together, and the losers answer 500. The spec asks
        // for 200 everywhere but orders to preserve and record a failure the characterization
        // exposes (no fix here). What must hold either way: at least one success and one row.
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(account.AccountId, 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => GetAsync(account.Subject, "/api/v1/gamification/dashboard")));

        Assert.Contains(responses, response => response.Status == HttpStatusCode.OK);
        Assert.All(
            responses,
            response => Assert.True(response.Status is HttpStatusCode.OK or HttpStatusCode.InternalServerError));
        await using var dbContext = Fixture.CreateDbContext();
        var rows = await dbContext.UserAchievements
            .Where(item => item.AccountId == account.AccountId)
            .ToArrayAsync();
        var row = Assert.Single(rows);
        Assert.Equal(UserAchievementStatus.Earned, row.Status);
        Assert.Equal(1, await Fixture.CountAsync("gamification_preferences", "account_id", account.AccountId));
    }

    [Fact]
    public async Task PublicBadges_ReconcileTheTargetAccountWhileBeingRead()
    {
        // Preserved on purpose (spec 0014, Q1 option A): the read still writes, through one step.
        var target = await Fixture.CreateAccountAsync();
        var viewer = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(target.AccountId, 1);
        Assert.Equal(0, await Fixture.CountAsync("user_achievements", "account_id", target.AccountId));
        Assert.Equal(0, await Fixture.CountAsync("gamification_preferences", "account_id", target.AccountId));

        var response = await GetAsync(viewer.Subject, $"/api/v1/gamification/users/{target.AccountId}/badges");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var now = Iso(Clock.GetUtcNow());
        AssertSameJson(
            $$"""
            {"isPublic":true,"badges":[{"code":"first-movie","title":"Primeiro filme","description":"Registre seu primeiro filme assistido.","firstEarnedAt":"{{now}}"}]}
            """,
            response.Body);
        Assert.Equal(1, await Fixture.CountAsync("user_achievements", "account_id", target.AccountId));
        Assert.Equal(1, await Fixture.CountAsync("gamification_preferences", "account_id", target.AccountId));
    }

    [Fact]
    public async Task PublicBadges_AreHiddenWhenTheOwnerMadeThemPrivate()
    {
        var target = await Fixture.CreateAccountAsync();
        var viewer = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(target.AccountId, 1);
        var update = await PutAsync(
            target.Subject,
            "/api/v1/gamification/preferences",
            new { badgesPublic = false, rankingEnabled = true });
        Assert.Equal(HttpStatusCode.OK, update.Status);

        var response = await GetAsync(viewer.Subject, $"/api/v1/gamification/users/{target.AccountId}/badges");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        AssertSameJson("""{"isPublic":false,"badges":[]}""", response.Body);
    }

    [Fact]
    public async Task PublicBadges_OfAnUnknownAccount_AreNotFoundWithTheBodyOfTheStatusCodePages()
    {
        var viewer = await Fixture.CreateAccountAsync();

        var response = await GetAsync(viewer.Subject, $"/api/v1/gamification/users/{Guid.NewGuid()}/badges");

        Assert.Equal(HttpStatusCode.NotFound, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var body = response.Json();
        Assert.Equal(404, body.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.GetProperty("title").GetString());
        Assert.False(body.TryGetProperty("detail", out _));
    }

    [Fact]
    public async Task UpdatePreferences_ReturnsAndPersistsTheNewValues()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await PutAsync(
            account.Subject,
            "/api/v1/gamification/preferences",
            new { badgesPublic = false, rankingEnabled = true });

        Assert.Equal(HttpStatusCode.OK, response.Status);
        AssertSameJson(
            $$"""{"badgesPublic":false,"rankingEnabled":true,"updatedAt":"{{Iso(Clock.GetUtcNow())}}"}""",
            response.Body);
        await using var dbContext = Fixture.CreateDbContext();
        var stored = await dbContext.GamificationPreferences.SingleAsync(
            item => item.AccountId == account.AccountId);
        Assert.False(stored.BadgesPublic);
        Assert.True(stored.RankingEnabled);
    }

    private static JsonElement Achievement(JsonElement dashboard, string code) =>
        dashboard.GetProperty("achievements").EnumerateArray()
            .Single(item => item.GetProperty("code").GetString() == code);

    private async Task<int> CountOtherAchievementsAsync(Guid accountId)
    {
        await using var dbContext = Fixture.CreateDbContext();
        var cinephile = await dbContext.AchievementDefinitions.SingleAsync(item => item.Code == "cinephile");
        return await dbContext.UserAchievements.CountAsync(
            item => item.AccountId == accountId && item.DefinitionId != cinephile.Id);
    }
}
