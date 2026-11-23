using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Gamification;
using Reelix.Infrastructure.Persistence.Gamification;

namespace Reelix.Infrastructure;

public static class GamificationInfrastructureModule
{
    public static IServiceCollection AddGamificationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IGamificationRepository, GamificationRepository>();
        return services;
    }
}
