using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Lists;
using Reelix.Infrastructure.Persistence.Lists;

namespace Reelix.Infrastructure;

public static class ListsDependencyInjection
{
    public static IServiceCollection AddListsInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserListsRepository, UserListsRepository>();
        return services;
    }
}
