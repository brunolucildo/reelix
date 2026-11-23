using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Notifications;

public static class NotificationsApplicationModule
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<NotificationsService>();
        services.AddScoped<SocialNotificationProcessor>();
        services.AddScoped<NotificationDeliveryProcessor>();
        return services;
    }
}
