using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Gamification;
using Reelix.Infrastructure.Persistence.Gamification;

namespace Reelix.Infrastructure;

public static class GamificationInfrastructureModule
{
    public static IServiceCollection AddGamificationInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IAchievementCatalog, AchievementCatalog>();
        services.AddScoped<IUserAchievements, UserAchievementStore>();
        services.AddScoped<IAchievementReconciliationTransaction, AchievementReconciliationTransaction>();
        services.AddScoped<IGamificationStatisticsReader, GamificationStatisticsReader>();
        services.AddScoped<IGamificationPreferencesStore, GamificationPreferencesStore>();
        services.AddScoped<IMonthlyRankingReader, MonthlyRankingReader>();
        services.AddScoped<IPublicBadgesReader, PublicBadgesReader>();
        return services;
    }
}
