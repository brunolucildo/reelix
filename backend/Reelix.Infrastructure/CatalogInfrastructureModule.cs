using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Catalog;
using Reelix.Infrastructure.Catalog;
using Reelix.Infrastructure.Persistence.Catalog;

namespace Reelix.Infrastructure;

public static class CatalogInfrastructureModule
{
    public static IServiceCollection AddCatalogInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IExternalCatalogReferenceRepository, ExternalCatalogReferenceRepository>();
        services.AddSingleton<ICatalogGenreCache, InMemoryCatalogGenreCache>();

        // A missing credential is valid at startup (the catalog answers 503 at runtime); only a
        // malformed one, such as a pasted line break or padding, stops the host from starting.
        services.AddOptions<TmdbCatalogOptions>()
            .Bind(configuration.GetSection(TmdbCatalogOptions.SectionName))
            .Validate(
                options => ConfiguredSecret.IsWellFormed(options.AccessToken),
                "Catalog:Tmdb:AccessToken must not contain control characters or leading/trailing spaces.")
            .ValidateOnStart();
        services.AddOptions<OmdbCatalogOptions>()
            .Bind(configuration.GetSection(OmdbCatalogOptions.SectionName))
            .Validate(
                options => ConfiguredSecret.IsWellFormed(options.ApiKey),
                "Catalog:Omdb:ApiKey must not contain control characters or leading/trailing spaces.")
            .ValidateOnStart();
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
        services.AddScoped<ICatalogGenreProvider>(serviceProvider =>
            serviceProvider.GetRequiredService<TmdbCatalogProvider>());
        return services;
    }
}

internal static class ConfiguredSecret
{
    /// <summary>
    /// True for an absent or empty value and for a value that is safe to send in a header: no
    /// control characters and no spaces at either end.
    /// </summary>
    public static bool IsWellFormed(string? value) =>
        string.IsNullOrEmpty(value) ||
        (value.Trim().Length == value.Length && !value.Any(char.IsControl));
}
