using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Authentication.GetAuthenticatedAccount>();
        return services;
    }
}
