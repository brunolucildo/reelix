using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Gamification;

public static class GamificationApplicationModule
{
    public static IServiceCollection AddGamificationApplication(this IServiceCollection services)
    {
        services.AddScoped<AchievementReconciler>();
        services.AddScoped<GetGamificationDashboardUseCase>();
        services.AddScoped<UpdateGamificationPreferencesUseCase>();
        services.AddScoped<GetGamificationStatisticsUseCase>();
        services.AddScoped<GetPublicBadgesUseCase>();
        services.AddScoped<GetMonthlyRankingUseCase>();
        return services;
    }
}
