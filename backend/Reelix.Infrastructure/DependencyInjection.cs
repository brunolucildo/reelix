using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Infrastructure.Catalog;
using Reelix.Infrastructure.Persistence.Catalog;
using Reelix.Infrastructure.Persistence;
using Reelix.Infrastructure.Persistence.Users;

namespace Reelix.Infrastructure;

public static class DependencyInjection
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

        services.AddDbContext<ReelixDbContext>(options =>
            options.UseNpgsql(connectionString));
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IExternalCatalogReferenceRepository, ExternalCatalogReferenceRepository>();
        services.Configure<TmdbCatalogOptions>(
            configuration.GetSection(TmdbCatalogOptions.SectionName));
        services.Configure<OmdbCatalogOptions>(
            configuration.GetSection(OmdbCatalogOptions.SectionName));
        services.AddHttpClient<TmdbCatalogProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api.themoviedb.org/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddHttpClient<OmdbCatalogProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.omdbapi.com/");
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<ICatalogProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbCatalogProvider>());
        services.AddScoped<ICatalogProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<OmdbCatalogProvider>());

        return services;
    }
}
