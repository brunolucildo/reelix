using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Gamification;

public static class GamificationApplicationModule
{
    public static IServiceCollection AddGamificationApplication(this IServiceCollection services)
    {
        services.AddScoped<GamificationService>();
        return services;
    }
}
