using System.Net;
using System.Text.Json;
using Reelix.UnitTests.Api;
using Reelix.UnitTests.Common;

namespace Reelix.UnitTests.Characterization;

public sealed record ApiResult(
    HttpStatusCode Status,
    string Body,
    string? ContentType,
    IReadOnlyDictionary<string, string> Headers)
{
    public JsonElement Json()
    {
        using var document = JsonDocument.Parse(Body);
        return document.RootElement.Clone();
    }
}

/// <summary>
/// Shared plumbing of the Batch 0 tests (spec 0014): calls the real host over HTTP as a given
/// account and keeps the clock at a known instant at the start of each test.
/// </summary>
public abstract class CharacterizationTestBase : IAsyncLifetime, IDisposable
{
    private readonly HttpClient _enabledClient;
    private readonly HttpClient _disabledClient;

    protected CharacterizationTestBase(PostgresCharacterizationFixture fixture)
    {
        Fixture = fixture;
        _enabledClient = fixture.PushEnabled.CreateClient();
        _disabledClient = fixture.PushDisabled.CreateClient();
    }

    protected PostgresCharacterizationFixture Fixture { get; }

    /// <summary>
    /// Starts every test from a known instant and a quiet outbox: work a previous test left open
    /// (pending deliveries, unprocessed social events) would otherwise be picked up by the next
    /// worker cycle.
    /// </summary>
    public async Task InitializeAsync()
    {
        Fixture.Clock.Set(FixedTimeProvider.DefaultNow);
        Fixture.Expo.Reset();
        Fixture.Expo.Responder = (_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError);
        await Fixture.ExecuteSqlAsync(
            "UPDATE public.notification_deliveries SET status = 'Cancelled' WHERE status IN ('Pending', 'Ticketed')");
        await Fixture.ExecuteSqlAsync(
            """
            INSERT INTO public.notification_processed_social_events (event_id, outcome, processed_at)
            SELECT s.id, 'obsolete', now()
            FROM public.social_events AS s
            WHERE NOT EXISTS (
                SELECT 1 FROM public.notification_processed_social_events AS p WHERE p.event_id = s.id)
            """);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    protected FixedTimeProvider Clock => Fixture.Clock;

    protected Task<ApiResult> GetAsync(Guid subject, string path) =>
        CallAsync(HttpMethod.Get, path, subject);

    protected Task<ApiResult> PutAsync(Guid subject, string path, object body) =>
        CallAsync(HttpMethod.Put, path, subject, body);

    protected async Task<ApiResult> CallAsync(
        HttpMethod method,
        string path,
        Guid subject,
        object? body = null,
        bool pushEnabled = true)
    {
        var factory = pushEnabled ? Fixture.PushEnabled : Fixture.PushDisabled;
        var client = pushEnabled ? _enabledClient : _disabledClient;
        using var request = factory.Request(method, path, subject, body);
        using var response = await client.SendAsync(request);
        var headers = response.Headers
            .Concat(response.Content.Headers)
            .ToDictionary(header => header.Key, header => string.Join(",", header.Value));
        return new ApiResult(
            response.StatusCode,
            await response.Content.ReadAsStringAsync(),
            response.Content.Headers.ContentType?.MediaType,
            headers);
    }

    protected static void AssertSameJson(string expected, string actual) =>
        HostJson.AssertSameJson(expected, actual);

    protected static string Iso(DateTimeOffset value) =>
        JsonSerializer.Serialize(value).Trim('"');

    public void Dispose()
    {
        _enabledClient.Dispose();
        _disabledClient.Dispose();
    }
}
