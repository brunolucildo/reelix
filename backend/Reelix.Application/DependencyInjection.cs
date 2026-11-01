using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Lists;
using Reelix.Application.Tracking;

namespace Reelix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Authentication.GetAuthenticatedAccount>();
        services.AddScoped<Catalog.CatalogService>();
        services.AddScoped<Catalog.CatalogGenreService>();
        services.AddScoped<Users.OnboardingService>();
        services.AddScoped<Users.UserProfileService>();
        services.AddTrackingApplication();
        services.AddListsApplication();
        return services;
    }
}
