using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reelix.Application.Ratings;
using Reelix.Infrastructure.Persistence.Ratings;
using Reelix.Infrastructure.Ratings;

namespace Reelix.Infrastructure;

public static class RatingsInfrastructureModule
{
    public static IServiceCollection AddRatingsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IRatingsCommentsRepository, RatingsCommentsRepository>();
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
                    .GetRequiredService<IOptions<OpenAiSpoilerOptions>>()
                    .Value;
                client.BaseAddress = new Uri("https://api.openai.com/v1/");
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });
        return services;
    }
}
