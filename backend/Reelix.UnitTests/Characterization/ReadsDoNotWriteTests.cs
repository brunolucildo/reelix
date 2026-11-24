using System.Net;

namespace Reelix.UnitTests.Characterization;

/// <summary>
/// Spec 0014 (R10 and R11): the reads of Gamification and Notifications that used to provision
/// rows lazily now write nothing. A command interceptor sees every SQL statement the host sends.
/// The dashboard and the public badges are NOT in this list on purpose (Q1, option A): they still
/// write, through the explicit reconciliation step.
/// </summary>
[Collection(PostgresCharacterizationCollection.Name)]
public sealed class ReadsDoNotWriteTests(PostgresCharacterizationFixture fixture)
    : CharacterizationTestBase(fixture)
{
    public static TheoryData<string> Reads => new()
    {
        "/api/v1/gamification/statistics?start=2030-06-01T00:00:00Z&end=2030-07-01T00:00:00Z",
        "/api/v1/gamification/friends/ranking?year=2030&month=6",
        "/api/v1/notifications/preferences",
        "/api/v1/notifications/inbox",
        "/api/v1/notifications/inbox/unread-count",
    };

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task ARead_ByASubjectWithoutAnAccount_AnswersAndWritesNothing(string path)
    {
        var subject = Guid.NewGuid();
        Fixture.Commands.Clear();

        var response = await GetAsync(subject, path);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Empty(Fixture.Commands.Writes());
        Assert.Equal(0, await Fixture.CountAsync("user_accounts", "supabase_user_id", subject));
    }

    [Theory]
    [MemberData(nameof(Reads))]
    public async Task ARead_ByAnAccountWithoutRows_AnswersAndWritesNothing(string path)
    {
        var account = await Fixture.CreateAccountAsync();
        Fixture.Commands.Clear();

        var response = await GetAsync(account.Subject, path);

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.Empty(Fixture.Commands.Writes());
        Assert.Equal(0, await Fixture.CountAsync("notification_preferences", "account_id", account.AccountId));
        Assert.Equal(0, await Fixture.CountAsync("gamification_preferences", "account_id", account.AccountId));
        Assert.Equal(0, await Fixture.CountAsync("user_achievements", "account_id", account.AccountId));
    }

    [Fact]
    public async Task NotificationPreferences_OfANewAccount_AreThreeFalsesWithoutCreatingARow()
    {
        var account = await Fixture.CreateAccountAsync();

        var response = await GetAsync(account.Subject, "/api/v1/notifications/preferences");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        AssertSameJson(
            """{"socialEnabled":false,"newEpisodeEnabled":false,"premiereEnabled":false}""",
            response.Body);
        Assert.Equal(0, await Fixture.CountAsync("notification_preferences", "account_id", account.AccountId));

        var update = await PutAsync(
            account.Subject,
            "/api/v1/notifications/preferences",
            new { socialEnabled = true, newEpisodeEnabled = false, premiereEnabled = true });
        var read = await GetAsync(account.Subject, "/api/v1/notifications/preferences");

        Assert.Equal(HttpStatusCode.OK, update.Status);
        AssertSameJson(
            """{"socialEnabled":true,"newEpisodeEnabled":false,"premiereEnabled":true}""",
            read.Body);
        Assert.Equal(1, await Fixture.CountAsync("notification_preferences", "account_id", account.AccountId));
    }

    [Fact]
    public async Task StatisticsAndRanking_OfASubjectWithoutAnAccount_AreThoseOfANewAccount()
    {
        var withoutAccount = Guid.NewGuid();
        var newAccount = await Fixture.CreateAccountAsync();
        const string statistics =
            "/api/v1/gamification/statistics?start=2030-06-01T00:00:00Z&end=2030-07-01T00:00:00Z";
        const string ranking = "/api/v1/gamification/friends/ranking?year=2030&month=6";

        var missingStatistics = await GetAsync(withoutAccount, statistics);
        var knownStatistics = await GetAsync(newAccount.Subject, statistics);
        var missingRanking = await GetAsync(withoutAccount, ranking);
        var knownRanking = await GetAsync(newAccount.Subject, ranking);

        AssertSameJson(knownStatistics.Body, missingStatistics.Body);
        AssertSameJson(knownRanking.Body, missingRanking.Body);
        Assert.Equal(HttpStatusCode.OK, missingStatistics.Status);
        Assert.Equal(HttpStatusCode.OK, missingRanking.Status);
    }

    [Fact]
    public async Task RemovingADeviceOrMarkingReadWithoutAnAccount_IsNotFoundAndCreatesNoAccount()
    {
        var subject = Guid.NewGuid();
        Fixture.Commands.Clear();

        var removed = await CallAsync(HttpMethod.Delete, $"/api/v1/notifications/devices/{Guid.NewGuid()}", subject);
        var marked = await PutAsync(subject, $"/api/v1/notifications/inbox/{Guid.NewGuid()}/read", new { });

        Assert.Equal(HttpStatusCode.NotFound, removed.Status);
        Assert.Equal(HttpStatusCode.NotFound, marked.Status);
        Assert.Empty(Fixture.Commands.Writes());
        Assert.Equal(0, await Fixture.CountAsync("user_accounts", "supabase_user_id", subject));
    }

    [Fact]
    public async Task TheDashboardStillReconcilesThroughTheExplicitStep_PreservedByQ1OptionA()
    {
        var account = await Fixture.CreateAccountAsync();
        Fixture.Commands.Clear();

        var response = await GetAsync(account.Subject, "/api/v1/gamification/dashboard");

        Assert.Equal(HttpStatusCode.OK, response.Status);
        Assert.NotEmpty(Fixture.Commands.Writes());
        Assert.Equal(1, await Fixture.CountAsync("gamification_preferences", "account_id", account.AccountId));
    }
}
