using Microsoft.Extensions.DependencyInjection;

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
        return services;
    }
}
