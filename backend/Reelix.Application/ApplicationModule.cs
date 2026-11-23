using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reelix.Application.Authentication;
using Reelix.Application.Catalog;
using Reelix.Application.Gamification;
using Reelix.Application.Lists;
using Reelix.Application.Notifications;
using Reelix.Application.Ratings;
using Reelix.Application.Social;
using Reelix.Application.Tracking;
using Reelix.Application.Users;

namespace Reelix.Application;

/// <summary>
/// Composition root of the Application layer: registers the shared clock and aggregates the
/// per-module registrations (<c>Add&lt;Module&gt;Application</c>); it registers nothing else.
/// </summary>
public static class ApplicationModule
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddAuthenticationApplication();
        services.AddCatalogApplication();
        services.AddUsersApplication();
        services.AddTrackingApplication();
        services.AddListsApplication();
        services.AddRatingsApplication();
        services.AddSocialApplication();
        services.AddNotificationsApplication();
        services.AddGamificationApplication();
        return services;
    }
}
