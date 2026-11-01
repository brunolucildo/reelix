using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Ratings;

public static class RatingsCommentsDependencyInjection
{
    public static IServiceCollection AddRatingsCommentsApplication(
        this IServiceCollection services)
    {
        services.AddScoped<RatingsCommentsService>();
        return services;
    }
}
