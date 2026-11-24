namespace Reelix.Application.Notifications;

/// <param name="Processed">How many deliveries were sent plus how many receipts were checked.</param>
public sealed record DeliverPushNotificationsResult(int Processed);
