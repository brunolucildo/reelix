using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Ratings;

public static class CommentModerationDependencyInjection
{
    public static IServiceCollection AddCommentModerationApplication(
        this IServiceCollection services)
    {
        services.AddScoped<CommentModerationProcessor>();
        return services;
    }
}
