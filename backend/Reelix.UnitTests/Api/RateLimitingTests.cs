using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;

namespace Reelix.UnitTests.Api;

/// <summary>
/// The four named rate limit policies (spec 0013, R10), registered once from a table: a synthetic
/// route per policy is rejected on the call after the limit, per caller.
/// </summary>
public sealed class RateLimitingTests : IAsyncLifetime
{
    private SyntheticApiHost _host = null!;

    public async Task InitializeAsync() =>
        _host = await SyntheticApiHost.StartAsync(app =>
        {
            foreach (var policy in new[]
                     {
                         "reviews-write", "social-search", "social-write", "notifications-write",
                     })
            {
                app.MapGet($"/limited/{policy}", () => Results.Ok())
                    .RequireRateLimiting(policy);
            }
        });

    public async Task DisposeAsync() => await _host.DisposeAsync();

    [Theory]
    [InlineData("reviews-write", 10)]
    [InlineData("social-search", 20)]
    [InlineData("social-write", 8)]
    [InlineData("notifications-write", 8)]
    public async Task Policy_RejectsTheCallAfterItsPermitsWithinTheMinute(string policy, int permits)
    {
        var subject = $"caller-{policy}";
        for (var call = 0; call < permits; call++)
        {
            using var accepted = await GetAsync(policy, subject);
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using var rejected = await GetAsync(policy, subject);

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        using var body = JsonDocument.Parse(await rejected.Content.ReadAsStringAsync());
        Assert.Equal(429, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "Limite de solicitações excedido.",
            body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Aguarde um minuto antes de tentar novamente.",
            body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Policy_CountsEachSubjectSeparately()
    {
        for (var call = 0; call < 8; call++)
        {
            using var accepted = await GetAsync("social-write", "first-caller");
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using var other = await GetAsync("social-write", "second-caller");
        using var exhausted = await GetAsync("social-write", "first-caller");

        Assert.Equal(HttpStatusCode.OK, other.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, exhausted.StatusCode);
    }

    [Fact]
    public async Task Policy_FallsBackToASharedBucketWhenThereIsNoSubjectAndNoAddress()
    {
        for (var call = 0; call < 8; call++)
        {
            using var accepted = await _host.Client.GetAsync("/limited/notifications-write");
            Assert.Equal(HttpStatusCode.OK, accepted.StatusCode);
        }

        using var rejected = await _host.Client.GetAsync("/limited/notifications-write");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    private Task<HttpResponseMessage> GetAsync(string policy, string subject)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/limited/{policy}");
        request.Headers.Add("X-Test-Subject", subject);
        return _host.Client.SendAsync(request);
    }
}
