using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reelix.Application.Catalog;
using Reelix.Application.Tracking;
using Reelix.Domain.Tracking;

namespace Reelix.UnitTests.Api;

/// <summary>
/// Characterizes the HTTP error contract of section 5 of spec 0013 that can be reached with fakes
/// (rows 1 to 6, 8, 10, 11, 12 and 13). Written against the code before the cross-cutting
/// foundation moved, so the contract can only change on purpose.
/// </summary>
public sealed class ErrorContractCharacterizationTests(ErrorContractApiFactory factory)
    : IClassFixture<ErrorContractApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Row1_ArgumentException_UsesUserMessageWithoutParameterSuffix()
    {
        using var response = await GetAsync(
            "/api/v1/catalog/search?query=matrix&type=movie&provider=tmdb&page=0");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Solicitação de catálogo inválida.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "A página deve estar entre 1 e 100.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row2_NotificationsInbox_UsesTheUserMessageWithoutTheParameterSuffix()
    {
        // Spec 0014 closed the gap: Notifications now goes through the shared mapper (ToUserMessage).
        using var response = await GetAsync("/api/v1/notifications/inbox?cursor=abc");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal("Cursor inválido.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O cursor da caixa de entrada é inválido.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row3_InvalidProvider_ReturnsTheFixedParameterProblem()
    {
        using var response = await GetAsync("/api/v1/catalog/search?query=matrix&provider=zzz");

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);
        Assert.Equal(
            "Provedor ou tipo de conteúdo inválido.",
            problem.GetProperty("title").GetString());
        Assert.Equal(
            "Use o provedor 'tmdb' ou 'omdb' e o tipo de conteúdo 'movie' ou 'series'.",
            problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row4_CatalogProviderError_CarriesTypeCodeAndRetryAfterWithoutDetail()
    {
        using var response = await GetAsync(
            "/api/v1/catalog/search?query=rate-limited&type=movie&provider=tmdb");

        var problem = await ReadProblemAsync(response, HttpStatusCode.TooManyRequests);
        Assert.Equal("O provedor limitou as consultas.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "https://reelix.local/problems/catalog_rate_limited",
            problem.GetProperty("type").GetString());
        Assert.Equal("catalog_rate_limited", problem.GetProperty("code").GetString());
        Assert.False(problem.TryGetProperty("detail", out _));
        Assert.Equal("3", Assert.Single(response.Headers.GetValues("Retry-After")));
    }

    [Fact]
    public async Task Row5_CatalogGenres_CarriesTypeButNoCodeAndNoRetryAfter()
    {
        using var response = await GetAsync("/api/v1/catalog/genres?type=movie&language=fr-FR");

        var problem = await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal("O catálogo está indisponível.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "https://reelix.local/problems/catalog_unavailable",
            problem.GetProperty("type").GetString());
        Assert.False(problem.TryGetProperty("code", out _));
        Assert.False(response.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Row6_TrackingCatalogError_HidesTheProviderMessage()
    {
        var seriesId = Guid.NewGuid();
        using var response = await GetAsync(
            $"/api/v1/tracking/series/{seriesId}/seasons/1/progress");

        var problem = await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal(
            "Não foi possível consultar o catálogo.",
            problem.GetProperty("title").GetString());
        Assert.Equal("Tente novamente mais tarde.", problem.GetProperty("detail").GetString());
        Assert.False(problem.TryGetProperty("code", out _));
        Assert.False(response.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Row8_NotFoundResult_IsRenderedByTheStatusCodePages()
    {
        using var response = await GetAsync("/api/v1/catalog/tmdb/movie/603");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal("Not Found", body.RootElement.GetProperty("title").GetString());
    }

    [Fact]
    public async Task Row10_DisabledPush_IsReportedAsServiceUnavailable()
    {
        using var response = await PostDeviceAsync(Guid.NewGuid());

        var problem = await ReadProblemAsync(response, HttpStatusCode.ServiceUnavailable);
        Assert.Equal("Notificações push indisponíveis.", problem.GetProperty("title").GetString());
        Assert.Equal(
            "O registro push está desabilitado até configurar o projeto e as credenciais Expo.",
            problem.GetProperty("detail").GetString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Row11_SubjectMissingOrInvalid_ReturnsUnauthorizedFromTheEndpoint(string? subject)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateTokenForSubject("valid", subject));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("WWW-Authenticate"));
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(401, body.RootElement.GetProperty("status").GetInt32());
    }

    [Fact]
    public async Task Row11_MissingJwt_IsChallengedByTheMiddleware()
    {
        using var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Contains("Bearer", response.Headers.WwwAuthenticate.ToString());
    }

    [Fact]
    public async Task Row12_RateLimiter_RejectsTheRequestAfterTheLimitWithAProblemBody()
    {
        var subject = Guid.NewGuid();
        for (var attempt = 0; attempt < 8; attempt++)
        {
            using var accepted = await PostDeviceAsync(subject);
            Assert.NotEqual(HttpStatusCode.TooManyRequests, accepted.StatusCode);
        }

        using var response = await PostDeviceAsync(subject);

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        // The rejection handler sets application/problem+json, but WriteAsJsonAsync replaces it:
        // the observable content type today is application/json.
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(429, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            "Limite de solicitações excedido.",
            body.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Aguarde um minuto antes de tentar novamente.",
            body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Row13_UnexpectedException_ReturnsAProblemWithoutInternalDetails()
    {
        using var response = await GetAsync(
            "/api/v1/catalog/search?query=boom&type=movie&provider=tmdb");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("internal secret detail", text);
        Assert.DoesNotContain("InvalidOperationException", text);
        Assert.DoesNotContain("   at ", text);
        using var body = JsonDocument.Parse(text);
        Assert.Equal(500, body.RootElement.GetProperty("status").GetInt32());
    }

    private Task<HttpResponseMessage> GetAsync(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken("valid", Guid.NewGuid()));
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> PostDeviceAsync(Guid subject)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/notifications/devices")
        {
            Content = JsonContent.Create(new
            {
                installationId = Guid.NewGuid(),
                expoPushToken = "ExponentPushToken[aaaaaaaaaaaaaaaaaaaaaa]",
                platform = "android",
                projectId = Guid.NewGuid(),
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factory.CreateToken("valid", subject));
        return _client.SendAsync(request);
    }

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatus)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(
            "application/problem+json",
            response.Content.Headers.ContentType?.MediaType);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }
}

public sealed class ErrorContractApiFactory : AuthenticationApiFactory
{
    protected override void ConfigureTestOverrides(IServiceCollection services)
    {
        services.RemoveAll<ICatalogProvider>();
        services.RemoveAll<ICatalogGenreProvider>();
        services.RemoveAll<ITrackingRepository>();
        services.RemoveAll<ITrackingCatalog>();
        services.AddSingleton<ICatalogProvider, FailingCatalogProvider>();
        services.AddSingleton<ICatalogGenreProvider, FailingGenreProvider>();
        services.AddSingleton<ITrackingRepository, ReferenceOnlyTrackingRepository>();
        services.AddSingleton<ITrackingCatalog, FailingTrackingCatalog>();
    }

    private sealed class FailingCatalogProvider : ICatalogProvider
    {
        public CatalogProvider Provider => CatalogProvider.Tmdb;

        public Task<ProviderCatalogPage> SearchAsync(
            string query,
            CatalogContentType? contentType,
            int page,
            string language,
            CancellationToken cancellationToken) =>
            query switch
            {
                "rate-limited" => throw new CatalogProviderException(
                    "catalog_rate_limited",
                    "O provedor limitou as consultas.",
                    429,
                    TimeSpan.FromSeconds(2.2)),
                "boom" => throw new InvalidOperationException("internal secret detail"),
                _ => Task.FromResult(new ProviderCatalogPage([], page, 1, 0)),
            };

        public Task<ProviderCatalogTitle?> GetDetailsAsync(
            CatalogContentType contentType,
            string externalId,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProviderCatalogTitle?>(null);

        public Task<ProviderCatalogSeason?> GetSeasonAsync(
            string externalSeriesId,
            int seasonNumber,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult<ProviderCatalogSeason?>(null);

        public Task<IReadOnlyList<ProviderCatalogSeasonSummary>?> GetSeasonsAsync(
            string externalSeriesId,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ProviderCatalogSeasonSummary>?>(null);
    }

    private sealed class FailingGenreProvider : ICatalogGenreProvider
    {
        public CatalogProvider Provider => CatalogProvider.Tmdb;

        public Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
            CatalogContentType contentType,
            string language,
            CancellationToken cancellationToken) =>
            throw new CatalogProviderException(
                "catalog_unavailable",
                "O catálogo está indisponível.",
                503,
                TimeSpan.FromSeconds(5));
    }

    private sealed class FailingTrackingCatalog : ITrackingCatalog
    {
        public Task<IReadOnlyList<CatalogSeasonSummary>?> GetSeasonsAsync(
            Guid seriesId,
            CatalogProvider provider,
            CancellationToken cancellationToken) =>
            throw new CatalogProviderException("catalog_unavailable", "Falha do provedor.", 503);

        public Task<CatalogSeason?> GetSeasonAsync(
            Guid seriesId,
            int seasonNumber,
            CatalogProvider provider,
            CancellationToken cancellationToken) =>
            throw new CatalogProviderException("catalog_unavailable", "Falha do provedor.", 503);
    }

    private sealed class ReferenceOnlyTrackingRepository : ITrackingRepository
    {
        public Task<bool> CatalogReferenceExistsAsync(
            Guid id,
            CatalogReferenceType type,
            CancellationToken cancellationToken) => Task.FromResult(true);

        public Task<string?> GetSeriesExternalIdAsync(
            Guid seriesId,
            CatalogProvider provider,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TrackingRecord?> GetAsync(
            Guid accountId,
            Guid targetId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TrackingRecord> AddOrUpdateAsync(
            TrackingRecord record,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RemoveAsync(
            TrackingRecord record,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TrackingPage<TrackingHistoryEntry>> GetHistoryAsync(
            Guid accountId,
            TrackingItemType? type,
            int page,
            int pageSize,
            TrackingHistoryOrder order,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<TrackingPage<TrackedSeries>> GetSeriesAsync(
            Guid accountId,
            int page,
            int pageSize,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<int> GetWatchedEpisodeCountAsync(
            Guid accountId,
            Guid seriesId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<HashSet<Guid>> GetWatchedEpisodeIdsAsync(
            Guid accountId,
            Guid seriesId,
            IReadOnlyCollection<Guid> episodeIds,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
