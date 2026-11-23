using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Authentication;

public static class AuthenticationApplicationModule
{
    public static IServiceCollection AddAuthenticationApplication(this IServiceCollection services)
    {
        services.AddScoped<GetAuthenticatedAccount>();
        return services;
    }
}
