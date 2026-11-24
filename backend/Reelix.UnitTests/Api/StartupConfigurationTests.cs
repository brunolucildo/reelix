using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Gamification;
using Reelix.Application.Lists;
using Reelix.Application.Notifications;
using Reelix.Application.Ratings;
using Reelix.Application.Social;
using Reelix.Application.Tracking;
using Reelix.Application.Users;

namespace Reelix.UnitTests.Api;

/// <summary>
/// Composition and startup (spec 0013, R1 and R9): the per-module registrations still resolve
/// every service, the clock is registered, and the TMDB and OMDb options are validated on start
/// without making a missing credential fatal.
/// </summary>
public sealed class StartupConfigurationTests(AuthenticationApiFactory factory)
    : IClassFixture<AuthenticationApiFactory>
{
    public static TheoryData<Type> ApplicationServices =>
    [
        typeof(GetAuthenticatedAccount),
        typeof(CatalogService),
        typeof(CatalogGenreService),
        typeof(OnboardingService),
        typeof(UserProfileService),
        typeof(TrackingService),
        typeof(UserListsService),
        typeof(RatingsCommentsService),
        typeof(CommentModerationProcessor),
        typeof(SocialService),
        typeof(GetNotificationPreferencesUseCase),
        typeof(UpdateNotificationPreferencesUseCase),
        typeof(RegisterNotificationDeviceUseCase),
        typeof(DeactivateNotificationDeviceUseCase),
        typeof(GetNotificationInboxUseCase),
        typeof(GetNotificationUnreadCountUseCase),
        typeof(MarkNotificationReadUseCase),
        typeof(ProcessSocialEventsUseCase),
        typeof(DeliverPushNotificationsUseCase),
        typeof(AchievementReconciler),
        typeof(GetGamificationDashboardUseCase),
        typeof(UpdateGamificationPreferencesUseCase),
        typeof(GetGamificationStatisticsUseCase),
        typeof(GetPublicBadgesUseCase),
        typeof(GetMonthlyRankingUseCase),
    ];

    [Theory]
    [MemberData(nameof(ApplicationServices))]
    public void EveryApplicationServiceIsRegisteredAndResolvable(Type serviceType)
    {
        using var scope = factory.Services.CreateScope();

        Assert.NotNull(scope.ServiceProvider.GetRequiredService(serviceType));
    }

    [Fact]
    public void TheSystemClockIsRegisteredOnceForApplicationAndInfrastructure()
    {
        Assert.Same(TimeProvider.System, factory.Services.GetRequiredService<TimeProvider>());
    }

    [Fact]
    public async Task HostWithoutCatalogCredentials_StartsAndTheCatalogAnswersServiceUnavailable()
    {
        await using var factoryWithoutCredentials = new CatalogWithoutCredentialsFactory();
        using var client = factoryWithoutCredentials.CreateClient();
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/v1/catalog/search?query=matrix&type=movie&provider=tmdb");
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            factoryWithoutCredentials.CreateToken("valid", Guid.NewGuid()));

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(
            "O catálogo não está configurado no servidor.",
            problem.RootElement.GetProperty("title").GetString());
    }

    [Theory]
    [InlineData("Catalog:Tmdb:AccessToken", "abc\ndef", "Catalog:Tmdb:AccessToken")]
    [InlineData("Catalog:Tmdb:AccessToken", " padded-token", "Catalog:Tmdb:AccessToken")]
    [InlineData("Catalog:Tmdb:AccessToken", "padded-token ", "Catalog:Tmdb:AccessToken")]
    [InlineData("Catalog:Omdb:ApiKey", "key\r", "Catalog:Omdb:ApiKey")]
    [InlineData("Catalog:Omdb:ApiKey", "key\u0000", "Catalog:Omdb:ApiKey")]
    public void HostWithAMalformedCatalogCredential_FailsToStart(
        string key,
        string value,
        string expectedInMessage)
    {
        using var factoryWithMalformedSecret = new CatalogWithoutCredentialsFactory(key, value);

        var exception = Assert.ThrowsAny<Exception>(() => factoryWithMalformedSecret.CreateClient());

        Assert.Contains(expectedInMessage, Flatten(exception));
    }

    [Theory]
    [InlineData("Catalog:Tmdb:AccessToken", "well-formed-test-credential")]
    [InlineData("Catalog:Omdb:ApiKey", "a1b2c3d4")]
    public void HostWithAWellFormedCatalogCredential_Starts(string key, string value)
    {
        using var factoryWithCredential = new CatalogWithoutCredentialsFactory(key, value);

        using var client = factoryWithCredential.CreateClient();

        Assert.NotNull(client);
    }

    private static string Flatten(Exception exception)
    {
        var messages = new List<string>();
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            messages.Add(current.Message);
            if (current is OptionsValidationException validation)
            {
                messages.AddRange(validation.Failures);
            }

            if (current.InnerException is null)
            {
                break;
            }
        }

        return string.Join(" | ", messages);
    }

    /// <summary>
    /// The real catalog providers (no fakes) with the TMDB and OMDb credentials forced to a value,
    /// so neither the developer's user secrets nor the environment leak into the test.
    /// </summary>
    private sealed class CatalogWithoutCredentialsFactory(
        string? key = null,
        string? value = null) : AuthenticationApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Catalog:Tmdb:AccessToken"] = string.Empty,
                    ["Catalog:Omdb:ApiKey"] = string.Empty,
                    [key ?? "Unused:Key"] = value,
                }));
        }

        protected override void ConfigureTestOverrides(IServiceCollection services)
        {
            services.RemoveAll<ICatalogProvider>();
            services.AddScoped<ICatalogProvider>(provider =>
                provider.GetRequiredService<Reelix.Infrastructure.Catalog.TmdbCatalogProvider>());
        }
    }
}
