using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Tracking;

public static class TrackingDependencyInjection
{
    public static IServiceCollection AddTrackingApplication(this IServiceCollection services)
    {
        services.AddScoped<TrackingService>();
        return services;
    }
}
