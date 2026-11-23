using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Social;

public static class SocialApplicationModule
{
    public static IServiceCollection AddSocialApplication(this IServiceCollection services)
    {
        services.AddScoped<SocialService>();
        return services;
    }
}
