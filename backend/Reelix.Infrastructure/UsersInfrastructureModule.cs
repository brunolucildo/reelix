using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Authentication;
using Reelix.Application.Users;
using Reelix.Infrastructure.Persistence.Users;

namespace Reelix.Infrastructure;

public static class UsersInfrastructureModule
{
    public static IServiceCollection AddUsersInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<IUserAccountRepository, UserAccountRepository>();
        services.AddScoped<IUserProfileRepository, UserProfileRepository>();
        services.AddScoped<IUserPreferencesRepository, UserPreferencesRepository>();
        return services;
    }
}
