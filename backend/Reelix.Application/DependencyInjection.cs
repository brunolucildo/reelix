using Microsoft.Extensions.DependencyInjection;
using Reelix.Application.Lists;
using Reelix.Application.Tracking;
using Reelix.Application.Ratings;
using Reelix.Application.Social;
using Reelix.Application.Notifications;
using Reelix.Application.Gamification;

namespace Reelix.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<Authentication.GetAuthenticatedAccount>();
        services.AddScoped<Catalog.CatalogService>();
        services.AddScoped<Catalog.CatalogGenreService>();
        services.AddScoped<Users.OnboardingService>();
        services.AddScoped<Users.UserProfileService>();
        services.AddTrackingApplication();
        services.AddListsApplication();
        services.AddRatingsCommentsApplication();
        services.AddCommentModerationApplication();
        services.AddScoped<SocialService>();
        services.AddScoped<NotificationsService>();
        services.AddScoped<GamificationService>();
        services.AddScoped<SocialNotificationProcessor>();
        services.AddScoped<NotificationDeliveryProcessor>();
        return services;
    }
}
