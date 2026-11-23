using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Users;

public static class UsersApplicationModule
{
    public static IServiceCollection AddUsersApplication(this IServiceCollection services)
    {
        services.AddScoped<OnboardingService>();
        services.AddScoped<UserProfileService>();
        return services;
    }
}
