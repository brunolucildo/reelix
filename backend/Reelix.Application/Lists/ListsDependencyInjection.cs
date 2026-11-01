using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Lists;

public static class ListsDependencyInjection
{
    public static IServiceCollection AddListsApplication(this IServiceCollection services)
    {
        services.AddScoped<UserListsService>();
        return services;
    }
}
