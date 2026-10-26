using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

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

    public string CreateToken(string scenario)
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
            [new Claim("sub", Guid.NewGuid().ToString())],
            notBefore,
            expires,
            credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        Environment.SetEnvironmentVariable("Supabase__Url", _previousSupabaseUrl);
        Environment.SetEnvironmentVariable("ConnectionStrings__Reelix", _previousConnectionString);
        Environment.SetEnvironmentVariable("Cors__AllowedOrigins__0", _previousCorsOrigin);
    }
}
