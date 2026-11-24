using Microsoft.Extensions.DependencyInjection;

namespace Reelix.Application.Notifications;

public static class NotificationsApplicationModule
{
    public static IServiceCollection AddNotificationsApplication(this IServiceCollection services)
    {
        services.AddScoped<GetNotificationPreferencesUseCase>();
        services.AddScoped<UpdateNotificationPreferencesUseCase>();
        services.AddScoped<RegisterNotificationDeviceUseCase>();
        services.AddScoped<DeactivateNotificationDeviceUseCase>();
        services.AddScoped<GetNotificationInboxUseCase>();
        services.AddScoped<GetNotificationUnreadCountUseCase>();
        services.AddScoped<MarkNotificationReadUseCase>();
        services.AddScoped<ProcessSocialEventsUseCase>();
        services.AddScoped<DeliverPushNotificationsUseCase>();
        return services;
    }
}
