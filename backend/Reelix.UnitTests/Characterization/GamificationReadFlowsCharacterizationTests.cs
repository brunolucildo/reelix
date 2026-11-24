using System.Net;
using System.Text.Json;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// Batch 0 of spec 0014 (R1 and R4): statistics and monthly ranking of the gamification module
/// over HTTP and PostgreSQL, with the exact JSON and the error responses of the current code.
/// </summary>
[Collection(PostgresCharacterizationCollection.Name)]
public sealed class GamificationReadFlowsCharacterizationTests(PostgresCharacterizationFixture fixture)
    : CharacterizationTestBase(fixture)
{
    [Fact]
    public async Task Statistics_ReturnsTheCompleteJsonContractForThePeriod()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(account.AccountId, 2, firstGenreId: 10, runtimeMinutes: 90);
        await Fixture.SeedEpisodesAsync(account.AccountId, 1, 3);
        var now = Iso(Clock.GetUtcNow());

        var response = await GetAsync(
            account.Subject,
            "/api/v1/gamification/statistics?start=2030-06-01T00:00:00Z&end=2030-07-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        AssertSameJson(
            $$"""
            {
              "periodStart": "2030-06-01T00:00:00+00:00", "periodEnd": "2030-07-01T00:00:00+00:00",
              "moviesWatched": 2, "episodesWatched": 3, "seriesExplored": 1, "genresExplored": 2,
              "moviesMissingGenreMetadata": 0, "itemsWithRuntime": 2, "itemsWithoutRuntime": 3,
              "estimatedWatchMinutes": 180, "estimatedWatchHours": 3, "runtimeSources": ["tmdb"],
              "runtimeMetadataVersion": 1, "runtimeMetadataUpdatedAt": "{{now}}",
              "confirmedWatchMinutes": null, "calculatedAt": "{{now}}", "runtimeCoverage": 0.4
            }
            """,
            response.Body);
    }

    [Fact]
    public async Task Statistics_ExcludeWhatWasWatchedOutsideThePeriod()
    {
        var account = await Fixture.CreateAccountAsync();
        await Fixture.SeedMoviesAsync(account.AccountId, 2);

        var response = await GetAsync(
            account.Subject,
            "/api/v1/gamification/statistics?start=2030-01-01T00:00:00Z&end=2030-02-01T00:00:00Z");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        var body = response.Json();
        Assert.Equal(0, body.GetProperty("moviesWatched").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("estimatedWatchMinutes").ValueKind);
        Assert.Equal(0, body.GetProperty("runtimeCoverage").GetDouble());
    }

    [Theory]
    [InlineData("2030-06-01T00:00:00Z", "2030-06-01T00:00:00Z")]
    [InlineData("2030-06-02T00:00:00Z", "2030-06-01T00:00:00Z")]
    [InlineData("2029-01-01T00:00:00Z", "2030-01-03T00:00:00Z")]
    public async Task Statistics_RejectsAnInvalidOrTooLongPeriod(string start, string end)
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await GetAsync(
            account.Subject,
            $"/api/v1/gamification/statistics?start={start}&end={end}");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var problem = response.Json();
        Assert.Equal("Período inválido.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O período deve ser válido e não pode exceder 366 dias.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Ranking_OfAViewerWhoOptedIn_FailsWithAServerErrorToday_KnownDefect()
    {
        // FINDING of spec 0014 (Batch 0): the ordering of the ranking page query over the
        // RankingCandidate projection cannot be translated by EF Core, so every ranking request of
        // a viewer with the ranking enabled answers 500. The spec orders to preserve the behavior
        // (no fix here). When the defect is fixed this assertion flips on purpose.
        var viewer = await Fixture.CreateAccountAsync();
        var friend = await Fixture.CreateAccountAsync();
        await Fixture.MakeFriendsAsync(viewer.AccountId, friend.AccountId);
        foreach (var account in new[] { viewer, friend })
        {
            await PutAsync(
                account.Subject,
                "/api/v1/gamification/preferences",
                new { badgesPublic = true, rankingEnabled = true });
        }

        await Fixture.SeedMoviesAsync(viewer.AccountId, 3);
        await Fixture.SeedMoviesAsync(friend.AccountId, 2);

        var response = await GetAsync(viewer.Subject, "/api/v1/gamification/friends/ranking?year=2030&month=6");

        Assert.Equal(HttpStatusCode.InternalServerError, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        Assert.Equal(500, response.Json().GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Ranking_IsDisabledUntilTheViewerOptsIn()
    {
        var viewer = await Fixture.CreateAccountAsync();

        var response = await GetAsync(viewer.Subject, "/api/v1/gamification/friends/ranking?year=2030&month=6");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        AssertSameJson(
            """{"isEnabled":false,"periodStart":"2030-06-01T00:00:00+00:00","periodEnd":"2030-07-01T00:00:00+00:00","totalParticipants":0,"items":[],"nextCursor":null}""",
            response.Body);
    }

    [Theory]
    [InlineData("year=1999&month=6", "Informe um ano e mês válidos.")]
    [InlineData("year=2030&month=13", "Informe um ano e mês válidos.")]
    [InlineData("year=2030&month=6&limit=0", "O limite deve estar entre 1 e 50.")]
    [InlineData("year=2030&month=6&limit=51", "O limite deve estar entre 1 e 50.")]
    // Spec 0014 (Q8): the " (Parameter 'cursor')" suffix is gone since the module migrated.
    [InlineData("year=2030&month=6&cursor=abc", "O cursor de paginação é inválido.")]
    public async Task Ranking_RejectsAnInvalidQuery(string query, string expectedDetail)
    {
        var viewer = await Fixture.CreateAccountAsync();

        var response = await GetAsync(viewer.Subject, $"/api/v1/gamification/friends/ranking?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.Status);
        Assert.Equal("application/problem+json", response.ContentType);
        var problem = response.Json();
        Assert.Equal("Consulta de ranking inválida.", problem.GetProperty("title").GetString());
        Assert.Equal(expectedDetail, problem.GetProperty("detail").GetString());
    }
}
