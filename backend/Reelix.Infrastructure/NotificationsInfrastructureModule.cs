using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Reelix.Application.Notifications;
using Reelix.Infrastructure.Notifications;
using Reelix.Infrastructure.Persistence.Notifications;

namespace Reelix.Infrastructure;

public static class NotificationsInfrastructureModule
{
    public static IServiceCollection AddNotificationsInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<INotificationPreferencesStore, NotificationPreferencesStore>();
        services.AddScoped<INotificationDevices, NotificationDeviceRegistry>();
        services.AddScoped<INotificationDeviceLookup, NotificationDeviceLookup>();
        services.AddScoped<INotificationDeliveries, NotificationDeliveryStore>();
        services.AddScoped<IUserNotifications, UserNotificationStore>();
        services.AddScoped<IFriendships, FriendshipLookup>();
        services.AddScoped<INotificationsUnitOfWork, NotificationsUnitOfWork>();
        services.AddScoped<ISocialEventOutbox, SocialEventOutbox>();
        services.AddScoped<IPushDeliveryQueue, PushDeliveryQueue>();
        services.AddSingleton<IJitterSource, RandomJitterSource>();
        services.AddOptions<ExpoPushOptions>()
            .Bind(configuration.GetSection(ExpoPushOptions.SectionName))
            .Validate(
                options => !options.Enabled ||
                    (options.ProjectId.HasValue &&
                     !string.IsNullOrWhiteSpace(options.AccessToken) &&
                     options.MaxAttempts is >= 1 and <= 10),
                "ExpoPush requires ProjectId, AccessToken and MaxAttempts between 1 and 10 when enabled.")
            .ValidateOnStart();
        services.AddSingleton<IPushRegistrationConfiguration>(serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<ExpoPushOptions>>().Value);
        services.AddHttpClient<IPushSender, ExpoPushSender>(client =>
        {
            client.BaseAddress = new Uri("https://exp.host/--/api/v2/");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        return services;
    }
}
