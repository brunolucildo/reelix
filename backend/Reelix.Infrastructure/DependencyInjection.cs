using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Lists;
using Reelix.Application.Tracking;
using Reelix.Application.Users;
using Reelix.Application.Ratings;
using Reelix.Application.Social;
using Reelix.Application.Notifications;
using Reelix.Application.Gamification;
using Reelix.Infrastructure.Catalog;
using Reelix.Infrastructure.Persistence.Catalog;
using Reelix.Infrastructure.Persistence.Tracking;
using Reelix.Infrastructure.Persistence;
using Reelix.Infrastructure.Persistence.Users;
using Reelix.Infrastructure.Persistence.Ratings;
using Reelix.Infrastructure.Persistence.Social;
using Reelix.Infrastructure.Persistence.Notifications;
using Reelix.Infrastructure.Persistence.Gamification;
using Reelix.Infrastructure.Notifications;
using Reelix.Infrastructure.Ratings;

namespace Reelix.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Reelix");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:Reelix with the PostgreSQL connection string.");
        }

        services.AddDbContext<ReelixDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddMemoryCache(options => options.SizeLimit = 200);
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        services.AddScoped<IExternalCatalogReferenceRepository, ExternalCatalogReferenceRepository>();
        services.AddScoped<IRatingsCommentsRepository, RatingsCommentsRepository>();
        services.AddScoped<ISocialRepository, SocialRepository>();
        services.AddScoped<INotificationsRepository, NotificationsRepository>();
        services.AddScoped<IGamificationRepository, GamificationRepository>();
        services.AddOptions<ExpoPushOptions>()
            .Bind(configuration.GetSection(ExpoPushOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    (options.ProjectId.HasValue &&
                     !string.IsNullOrWhiteSpace(options.AccessToken) &&
                     options.MaxAttempts is >= 1 and <= 10),
                "ExpoPush requires ProjectId, AccessToken and MaxAttempts between 1 and 10 when enabled.")
            .ValidateOnStart();
        services.AddSingleton<IPushRegistrationConfiguration>(serviceProvider =>
            serviceProvider.GetRequiredService<
                Microsoft.Extensions.Options.IOptions<ExpoPushOptions>>().Value);
        services.AddHttpClient<IExpoPushClient, ExpoPushClient>(client =>
        {
            client.BaseAddress = new Uri("https://exp.host/--/api/v2/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddOptions<OpenAiSpoilerOptions>()
            .Bind(configuration.GetSection(OpenAiSpoilerOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    options.TemporarilyDisabled ||
                    (!string.IsNullOrWhiteSpace(options.ApiKey) &&
                     !string.IsNullOrWhiteSpace(options.Model)),
                "OpenAI:SpoilerAnalysis requires an API key and model when enabled.")
            .Validate(
                options => options.TimeoutSeconds is >= 1 and <= 30,
                "OpenAI:SpoilerAnalysis:TimeoutSeconds must be between 1 and 30.")
            .ValidateOnStart();
        services.AddSingleton<ICommentModerationConfiguration, OpenAiCommentModerationConfiguration>();
        services.AddHttpClient<ICommentSpoilerClassifier, OpenAiCommentSpoilerClassifier>(
            (serviceProvider, client) =>
            {
                var options = serviceProvider
                    .GetRequiredService<Microsoft.Extensions.Options.IOptions<OpenAiSpoilerOptions>>()
                    .Value;
                client.BaseAddress = new Uri("https://api.openai.com/v1/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
        services.AddTrackingInfrastructure();
        services.AddListsInfrastructure();
        services.Configure<TmdbCatalogOptions>(
            configuration.GetSection(TmdbCatalogOptions.SectionName));
        services.Configure<OmdbCatalogOptions>(
            configuration.GetSection(OmdbCatalogOptions.SectionName));
        services.AddHttpClient<TmdbCatalogProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<OmdbCatalogProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.omdbapi.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<ICatalogProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbCatalogProvider>());
        services.AddScoped<ICatalogProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<OmdbCatalogProvider>());
        services.AddScoped<ICatalogGenreProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbCatalogProvider>());

        return services;
    }
}
