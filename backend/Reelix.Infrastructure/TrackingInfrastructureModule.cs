using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Tracking;
using Reelix.Infrastructure.Persistence.Tracking;

namespace Reelix.Infrastructure;

public static class TrackingInfrastructureModule
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
