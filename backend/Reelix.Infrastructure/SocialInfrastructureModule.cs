using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Social;
using Reelix.Infrastructure.Persistence.Social;

namespace Reelix.Infrastructure;

public static class SocialInfrastructureModule
{
    public static IServiceCollection AddSocialInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<ISocialRepository, SocialRepository>();
        return services;
    }
}
