using System.Text.Json;

namespace Reelix.Application.Notifications;

public sealed class NotificationDeliveryProcessor(
    INotificationsRepository repository,
    IExpoPushClient expoPushClient,
    IPushRegistrationConfiguration configuration)
{
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        if (!configuration.IsEnabled)
        {
            return 0;
        }

        var now = DateTimeOffset.UtcNow;
        var deliveries = await repository.ClaimPushDeliveriesAsync(
            50,
            now,
            TimeSpan.FromMinutes(2),
            cancellationToken);
        foreach (var delivery in deliveries)
        {
            ExpoPushResponse response;
            try
            {
                response = await expoPushClient.SendAsync(delivery, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception) when (
                exception is HttpRequestException or TaskCanceledException or JsonException)
            {
                response = new ExpoPushResponse(
                    false,
                    null,
                    exception is TaskCanceledException ? "expo_timeout" : "expo_transport_error",
                    null,
                    false,
                    true);
            }

            await repository.RecordPushResponseAsync(
                delivery,
                response,
                configuration.MaxAttempts,
                cancellationToken);
        }

        var receipts = await repository.ClaimReceiptChecksAsync(50, DateTimeOffset.UtcNow, cancellationToken);
        foreach (var receipt in receipts)
        {
            var response = await expoPushClient.GetReceiptAsync(receipt.TicketId, cancellationToken);
            await repository.RecordReceiptResponseAsync(
                receipt,
                response,
                DateTimeOffset.UtcNow,
                cancellationToken);
        }

        return deliveries.Count + receipts.Count;
    }
}
