using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reelix.Infrastructure.Persistence;

namespace Reelix.Infrastructure;

/// <summary>
/// Composition root of the Infrastructure layer: registers the database, the shared clock and the
/// memory cache, and aggregates the per-module registrations (<c>Add&lt;Module&gt;Infrastructure</c>).
/// </summary>
public static class InfrastructureModule
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Reelix");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Configure ConnectionStrings:Reelix with the PostgreSQL connection string.");
        }

        services.TryAddSingleton(TimeProvider.System);
        services.AddDbContext<ReelixDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddMemoryCache(options => options.SizeLimit = 200);
        services.AddUsersInfrastructure();
        services.AddRatingsInfrastructure(configuration);
        services.AddSocialInfrastructure();
        services.AddNotificationsInfrastructure(configuration);
        services.AddGamificationInfrastructure();
        services.AddTrackingInfrastructure();
        services.AddListsInfrastructure();
        services.AddCatalogInfrastructure(configuration);

        return services;
    }
}
