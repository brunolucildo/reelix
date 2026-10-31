using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Reelix.Application.Catalog;
using Reelix.Application.Authentication;
using Reelix.Application.Users;
using Reelix.Domain.Users;

namespace Reelix.UnitTests.Api;

public sealed class AuthenticationEndpointTests
    : IClassFixture<AuthenticationApiFactory>
{
    private readonly HttpClient _client;
    private readonly AuthenticationApiFactory _factory;

    public AuthenticationEndpointTests(AuthenticationApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AccountEndpoint_RejectsAnonymousRequest()
    {
        using var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerUi_ExposesCatalogEndpointsInDevelopment()
    {
        using var response = await _client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var document = await response.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/catalog/search", document);
        Assert.Contains("/api/v1/onboarding", document);
        Assert.Contains("/api/v1/profile", document);
        Assert.Contains("Bearer", document);
    }

    [Theory]
    [InlineData("/api/v1/onboarding")]
    [InlineData("/api/v1/profile")]
    [InlineData("/api/v1/catalog/genres?type=movie")]
    public async Task UserEndpoints_RejectAnonymousRequests(string path)
    {
        using var response = await _client.GetAsync(path);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProfileEndpoint_ReturnsPrivateDefaultsAndUpdatesOnlyProfileFields()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profile");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Profile));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Null(document.RootElement.GetProperty("displayName").GetString());
        Assert.False(document.RootElement.GetProperty("profileIsPublic").GetBoolean());
        Assert.False(document.RootElement.GetProperty("favoritesArePublic").GetBoolean());
        Assert.False(document.RootElement.GetProperty("watchHistoryIsPublic").GetBoolean());
        Assert.False(document.RootElement.TryGetProperty("email", out _));

        using var update = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new
            {
                displayName = "  Cinefila  ",
                biography = "Gosto de cinema.",
                profileIsPublic = true,
                favoritesArePublic = false,
                watchHistoryIsPublic = false,
            }),
        };
        update.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Profile));

        using var updateResponse = await _client.SendAsync(update);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        using var updated = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        Assert.Equal("Cinefila", updated.RootElement.GetProperty("displayName").GetString());
        Assert.True(updated.RootElement.GetProperty("profileIsPublic").GetBoolean());
    }

    [Fact]
    public async Task ProfileEndpoint_RejectsDisplayNameAboveLimit()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
        {
            Content = JsonContent.Create(new
            {
                displayName = new string('x', 51),
                biography = (string?)null,
                profileIsPublic = false,
                favoritesArePublic = false,
                watchHistoryIsPublic = false,
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Profile));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PublicProfileEndpoint_EnforcesProfileAndFavoriteVisibility()
    {
        using var preferencesRequest = new HttpRequestMessage(HttpMethod.Put, "/api/v1/onboarding/progress")
        {
            Content = JsonContent.Create(new
            {
                currentStep = "favoriteMovies",
                movieGenreIds = Array.Empty<int>(),
                seriesGenreIds = Array.Empty<int>(),
                favoriteMovieIds = new[] { CatalogEndpointTestServices.TitleId },
                favoriteSeriesIds = Array.Empty<Guid>(),
            }),
        };
        preferencesRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.PublicProfile));
        using var preferencesResponse = await _client.SendAsync(preferencesRequest);
        Assert.Equal(HttpStatusCode.OK, preferencesResponse.StatusCode);

        var profilePath = $"/api/v1/profile/{_factory.GetAccountId(TestUserIds.PublicProfile)}";
        using var privateRequest = new HttpRequestMessage(HttpMethod.Get, profilePath);
        privateRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Onboarding));
        using var privateResponse = await _client.SendAsync(privateRequest);
        Assert.Equal(HttpStatusCode.NotFound, privateResponse.StatusCode);

        await UpdateProfileAsync(profileIsPublic: true, favoritesArePublic: false);
        using var publicRequest = new HttpRequestMessage(HttpMethod.Get, profilePath);
        publicRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Onboarding));
        using var publicResponse = await _client.SendAsync(publicRequest);
        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        using var publicProfile = JsonDocument.Parse(await publicResponse.Content.ReadAsStringAsync());
        Assert.Empty(publicProfile.RootElement.GetProperty("favoriteMovieIds").EnumerateArray());
        Assert.False(publicProfile.RootElement.TryGetProperty("email", out _));

        await UpdateProfileAsync(profileIsPublic: true, favoritesArePublic: true);
        using var favoritesRequest = new HttpRequestMessage(HttpMethod.Get, profilePath);
        favoritesRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Onboarding));
        using var favoritesResponse = await _client.SendAsync(favoritesRequest);
        using var profileWithFavorites = JsonDocument.Parse(await favoritesResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            CatalogEndpointTestServices.TitleId,
            Assert.Single(profileWithFavorites.RootElement.GetProperty("favoriteMovieIds").EnumerateArray()).GetGuid());

        async Task UpdateProfileAsync(bool profileIsPublic, bool favoritesArePublic)
        {
            using var update = new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile")
            {
                Content = JsonContent.Create(new
                {
                    displayName = "Cinefila",
                    biography = "Gosto de cinema.",
                    profileIsPublic,
                    favoritesArePublic,
                    watchHistoryIsPublic = false,
                }),
            };
            update.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _factory.CreateToken("valid", TestUserIds.PublicProfile));
            using var response = await _client.SendAsync(update);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task OnboardingEndpoint_PersistsCompletionForAuthenticatedAccount()
    {
        using var completeRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/onboarding/complete");
        completeRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Onboarding));

        using var completeResponse = await _client.SendAsync(completeRequest);

        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        using var complete = JsonDocument.Parse(await completeResponse.Content.ReadAsStringAsync());
        Assert.True(complete.RootElement.GetProperty("isComplete").GetBoolean());

        using var getRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/onboarding");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Onboarding));
        using var getResponse = await _client.SendAsync(getRequest);

        using var saved = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        Assert.True(saved.RootElement.GetProperty("isComplete").GetBoolean());
        Assert.Equal("completed", saved.RootElement.GetProperty("currentStep").GetString());
    }

    [Fact]
    public async Task OnboardingEndpoint_RejectsFavoriteTitleMissingFromCatalog()
    {
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/v1/onboarding/progress")
        {
            Content = JsonContent.Create(new
            {
                currentStep = "favoriteMovies",
                movieGenreIds = Array.Empty<int>(),
                seriesGenreIds = Array.Empty<int>(),
                favoriteMovieIds = new[] { Guid.NewGuid() },
                favoriteSeriesIds = Array.Empty<Guid>(),
            }),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid", TestUserIds.Onboarding));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DevelopmentAuthHelper_ProvidesLocalPageAndSupabaseUrl()
    {
        using var pageResponse = await _client.GetAsync("/dev-auth.html");
        Assert.Equal(HttpStatusCode.OK, pageResponse.StatusCode);
        Assert.Contains("Gerar token para o Swagger", await pageResponse.Content.ReadAsStringAsync());

        using var configResponse = await _client.GetAsync("/api/v1/dev/auth/config");
        Assert.Equal(HttpStatusCode.OK, configResponse.StatusCode);
        using var config = JsonDocument.Parse(await configResponse.Content.ReadAsStringAsync());
        Assert.Equal(
            "https://reelix-test.supabase.co",
            config.RootElement.GetProperty("supabaseUrl").GetString());
    }

    [Fact]
    public async Task CatalogSearch_RequiresAuthenticationAndReturnsNormalizedTitle()
    {
        using var anonymousResponse = await _client.GetAsync(
            "/api/v1/catalog/search?query=matrix&type=movie&provider=tmdb");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);

        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/catalog/search?query=matrix&type=movie&provider=tmdb");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken("valid"));

        using var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var title = Assert.Single(document.RootElement.GetProperty("items").EnumerateArray());
        Assert.Equal(
            CatalogEndpointTestServices.TitleId,
            title.GetProperty("id").GetGuid());
        Assert.Equal("tmdb", title.GetProperty("provider").GetString());
        Assert.Equal("movie", title.GetProperty("contentType").GetString());
        Assert.Equal("A Test Movie", title.GetProperty("title").GetString());
    }

    [Fact]
    public async Task AccountEndpoint_AllowsPreflightFromConfiguredWebOrigin()
    {
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/v1/auth/me");
        request.Headers.Add("Origin", "http://localhost:8081");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(
            "http://localhost:8081",
            response.Headers.GetValues("Access-Control-Allow-Origin").Single());
        Assert.Contains(
            "authorization",
            response.Headers.GetValues("Access-Control-Allow-Headers").Single()
                .ToLowerInvariant());
    }

    [Theory]
    [InlineData("invalid-signature")]
    [InlineData("expired")]
    [InlineData("wrong-issuer")]
    [InlineData("wrong-audience")]
    public async Task AccountEndpoint_RejectsInvalidJwt(string tokenScenario)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/auth/me");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            _factory.CreateToken(tokenScenario));

        using var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public void JwtBearerConfiguration_ValidatesSignatureIssuerAudienceAndLifetime()
    {
        var options = _factory.Services
            .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
            .Get(JwtBearerDefaults.AuthenticationScheme);
        var validation = options.TokenValidationParameters;

        Assert.True(validation.ValidateIssuerSigningKey);
        Assert.True(validation.ValidateIssuer);
        Assert.True(validation.ValidateAudience);
        Assert.True(validation.ValidateLifetime);
        Assert.Equal("https://reelix-test.supabase.co/auth/v1", validation.ValidIssuer);
        Assert.Equal("authenticated", validation.ValidAudience);
        Assert.Contains(SecurityAlgorithms.EcdsaSha256, validation.ValidAlgorithms!);
        Assert.Contains(SecurityAlgorithms.RsaSha256, validation.ValidAlgorithms!);
        Assert.Contains(SecurityAlgorithms.HmacSha256, validation.ValidAlgorithms!);
    }
}

public sealed class AuthenticationApiFactory : WebApplicationFactory<Program>
{
    private static readonly RSA SigningRsa = RSA.Create(2048);
    private static readonly RSA ForeignRsa = RSA.Create(2048);

    private readonly string? _previousSupabaseUrl =
        Environment.GetEnvironmentVariable("Supabase__Url");
    private readonly string? _previousConnectionString =
        Environment.GetEnvironmentVariable("ConnectionStrings__Reelix");
    private readonly string? _previousCorsOrigin =
        Environment.GetEnvironmentVariable("Cors__AllowedOrigins__0");

    public AuthenticationApiFactory()
    {
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", "http://localhost:8081");
        Environment.SetEnvironmentVariable("Supabase__Url", "https://reelix-test.supabase.co");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Reelix",
            "Host=localhost;Database=reelix_test;Username=test;Password=test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICatalogProvider>();
            services.RemoveAll<ICatalogGenreProvider>();
            services.RemoveAll<IExternalCatalogReferenceRepository>();
            services.RemoveAll<IUserAccountRepository>();
            services.RemoveAll<IUserProfileRepository>();
            services.RemoveAll<IUserPreferencesRepository>();
            services.AddSingleton<ICatalogProvider, CatalogEndpointTestServices.TmdbProvider>();
            services.AddSingleton<ICatalogGenreProvider, CatalogEndpointTestServices.TmdbGenreProvider>();
            services.AddSingleton<IExternalCatalogReferenceRepository,
                CatalogEndpointTestServices.ReferenceRepository>();
            services.AddSingleton<IUserAccountRepository, TestUserAccountRepository>();
            services.AddSingleton<IUserProfileRepository, TestUserProfileRepository>();
            services.AddSingleton<IUserPreferencesRepository, TestUserPreferencesRepository>();
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = null;
                    options.ConfigurationManager = null;
                    options.TokenValidationParameters.IssuerSigningKey =
                        new RsaSecurityKey(SigningRsa);
                });
        });
    }

    public string CreateToken(string scenario, Guid? subject = null)
    {
        var issuer = scenario == "wrong-issuer"
            ? "https://untrusted.example/auth/v1"
            : "https://reelix-test.supabase.co/auth/v1";
        var audience = scenario == "wrong-audience" ? "untrusted" : "authenticated";
        var expires = scenario == "expired"
            ? DateTime.UtcNow.AddMinutes(-2)
            : DateTime.UtcNow.AddMinutes(5);
        var notBefore = scenario == "expired"
            ? DateTime.UtcNow.AddMinutes(-5)
            : DateTime.UtcNow.AddMinutes(-1);
        var rsa = scenario == "invalid-signature" ? ForeignRsa : SigningRsa;
        var credentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256);
        var token = new JwtSecurityToken(
            issuer,
            audience,
            [new Claim("sub", (subject ?? Guid.NewGuid()).ToString())],
            notBefore,
            expires,
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Guid GetAccountId(Guid supabaseUserId) =>
        ((TestUserAccountRepository)Services.GetRequiredService<IUserAccountRepository>())
            .GetAccountId(supabaseUserId);

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("Supabase__Url", _previousSupabaseUrl);
        Environment.SetEnvironmentVariable("ConnectionStrings__Reelix", _previousConnectionString);
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", _previousCorsOrigin);
    }
}

internal static class TestUserIds
{
    public static readonly Guid Profile = Guid.Parse("e579f427-753f-4fb9-80cb-ea06479a0921");

    public static readonly Guid Onboarding = Guid.Parse("f132b735-3f53-408f-8801-56e3dd894740");

    public static readonly Guid PublicProfile = Guid.Parse("60c6547d-8193-47e0-8d55-83400cdac6ea");
}

internal sealed class TestUserAccountRepository : IUserAccountRepository
{
    private readonly ConcurrentDictionary<Guid, UserAccount> accounts = new();

    public Task<UserAccount> GetOrCreateAsync(Guid supabaseUserId, CancellationToken cancellationToken) =>
        Task.FromResult(accounts.GetOrAdd(supabaseUserId, UserAccount.Create));

    public Guid GetAccountId(Guid supabaseUserId) =>
        accounts.GetOrAdd(supabaseUserId, UserAccount.Create).Id;
}

internal sealed class TestUserProfileRepository : IUserProfileRepository
{
    private readonly ConcurrentDictionary<Guid, UserProfile> profiles = new();

    public Task<UserProfile> GetOrCreateAsync(Guid accountId, CancellationToken cancellationToken) =>
        Task.FromResult(profiles.GetOrAdd(accountId, UserProfile.Create));

    public Task<UserProfile?> GetPublicAsync(Guid accountId, CancellationToken cancellationToken)
    {
        profiles.TryGetValue(accountId, out var profile);
        return Task.FromResult(profile?.ProfileIsPublic == true ? profile : null);
    }

    public Task SaveAsync(UserProfile profile, CancellationToken cancellationToken)
    {
        profiles[profile.AccountId] = profile;
        return Task.CompletedTask;
    }
}

internal sealed class TestUserPreferencesRepository : IUserPreferencesRepository
{
    private readonly ConcurrentDictionary<Guid, OnboardingPreferences> preferences = new();

    public Task<OnboardingPreferences?> GetExistingAsync(
        Guid accountId,
        CancellationToken cancellationToken)
    {
        preferences.TryGetValue(accountId, out var saved);
        return Task.FromResult(saved);
    }

    public Task<OnboardingPreferences> GetOrCreateAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        Task.FromResult(preferences.GetOrAdd(accountId, _ => EmptyPreferences()));

    public Task SaveProgressAsync(
        Guid accountId,
        SaveOnboardingProgress progress,
        CancellationToken cancellationToken)
    {
        preferences[accountId] = new OnboardingPreferences(
            false,
            progress.CurrentStep,
            progress.MovieGenreIds,
            progress.SeriesGenreIds,
            progress.FavoriteMovieIds,
            progress.FavoriteSeriesIds);
        return Task.CompletedTask;
    }

    public Task CompleteAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var current = preferences.GetOrAdd(accountId, _ => EmptyPreferences());
        preferences[accountId] = current with
        {
            IsComplete = true,
            CurrentStep = OnboardingStep.Completed,
        };
        return Task.CompletedTask;
    }

    public Task<bool> CatalogTitlesExistAsync(
        IReadOnlyCollection<Guid> titleIds,
        CatalogContentType contentType,
        CancellationToken cancellationToken) =>
        Task.FromResult(titleIds.All(titleId => titleId == CatalogEndpointTestServices.TitleId));

    private static OnboardingPreferences EmptyPreferences() => new(
        false,
        OnboardingStep.Welcome,
        [],
        [],
        [],
        []);
}

internal static class CatalogEndpointTestServices
{
    public static readonly Guid TitleId = Guid.Parse("972f0e2f-ae08-4514-9c46-9e45ea4be8b6");

    public sealed class TmdbProvider : ICatalogProvider
    {
        public CatalogProvider Provider => CatalogProvider.Tmdb;

        public Task<ProviderCatalogPage> SearchAsync(
            string query,
            CatalogContentType? contentType,
            int page,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult(new ProviderCatalogPage(
                [
                    new ProviderCatalogTitle(
                        "11",
                        CatalogContentType.Movie,
                        "A Test Movie",
                        null,
                        null,
                        [],
                        null,
                        null,
                        null,
                        null,
                        null),
                ],
                page,
                1,
                1));

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
    }

    public sealed class TmdbGenreProvider : ICatalogGenreProvider
    {
        public CatalogProvider Provider => CatalogProvider.Tmdb;

        public Task<IReadOnlyList<CatalogGenre>> GetGenresAsync(
            CatalogContentType contentType,
            string language,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<CatalogGenre>>([new CatalogGenre(28, "Ação")]);
    }

    public sealed class ReferenceRepository : IExternalCatalogReferenceRepository
    {
        public Task<IReadOnlyDictionary<CatalogReferenceKey, Guid>> GetOrCreateIdsAsync(
            CatalogProvider provider,
            IReadOnlyCollection<CatalogReferenceKey> references,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<CatalogReferenceKey, Guid>>(
                references.ToDictionary(reference => reference, _ => TitleId));
    }
}
