using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Catalog;

public static class CatalogApplicationModule
{
    public static IServiceCollection AddCatalogApplication(this IServiceCollection services)
    {
        services.AddScoped<CatalogService>();
        services.AddScoped<CatalogGenreService>();
        return services;
    }
}
