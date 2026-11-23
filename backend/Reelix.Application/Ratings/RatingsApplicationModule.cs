using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Ratings;

public static class RatingsApplicationModule
{
    public static IServiceCollection AddRatingsApplication(this IServiceCollection services)
    {
        services.AddScoped<RatingsCommentsService>();
        services.AddScoped<CommentModerationProcessor>();
        return services;
    }
}
