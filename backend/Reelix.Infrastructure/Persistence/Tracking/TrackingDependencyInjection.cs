using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Tracking;

namespace Reelix.Infrastructure.Persistence.Tracking;

public static class TrackingDependencyInjection
{
    public static IServiceCollection AddTrackingInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<TrackingRepository>();
        services.AddScoped<ITrackingRepository>(provider =>
            provider.GetRequiredService<TrackingRepository>());
        services.AddScoped<IEpisodeTrackingLookup>(provider =>
            provider.GetRequiredService<TrackingRepository>());
        services.AddScoped<ITrackingCatalog, TrackingCatalog>();
        return services;
    }
}
