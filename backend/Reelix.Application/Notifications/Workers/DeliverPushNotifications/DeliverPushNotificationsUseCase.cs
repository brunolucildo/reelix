using Reelix.Domain.Notifications;

namespace Reelix.Application.Notifications;

/// <summary>
/// One cycle of the push delivery worker: sends the pending deliveries that are due, then reads the
/// receipts that are due. The timing rules live in <see cref="PushRetryPolicy"/> and the
/// eligibility rule in <see cref="PushDeliveryEligibility"/>.
/// </summary>
/// <remarks>
/// A failure to read a receipt is not caught: the cycle stops and the worker repeats it (the work
/// already committed stays committed). This is preserved behavior, not an improvement.
/// </remarks>
public sealed class DeliverPushNotificationsUseCase(
    IPushRegistrationConfiguration configuration,
    IPushDeliveryQueue queue,
    IPushSender sender,
    INotificationDeliveries deliveries,
    INotificationDeviceLookup devices,
    INotificationsUnitOfWork unitOfWork,
    IJitterSource jitter,
    TimeProvider clock)
{
    /// <summary>Deliveries sent, and receipts checked, per cycle.</summary>
    public const int BatchSize = 50;

    public async Task<DeliverPushNotificationsResult> ExecuteAsync(
        DeliverPushNotificationsCommand command,
        CancellationToken cancellationToken)
    {
        if (!configuration.IsEnabled)
        {
            return new DeliverPushNotificationsResult(0);
        }

        var sent = await SendPendingDeliveriesAsync(cancellationToken);
        var checkedReceipts = await CheckReceiptsAsync(cancellationToken);
        return new DeliverPushNotificationsResult(sent + checkedReceipts);
    }

    private async Task<int> SendPendingDeliveriesAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var work = new List<(NotificationDelivery Delivery, Guid LeaseId, PushMessage Message, Guid DeviceId)>();
        await using (var batch = await queue.ReservePendingAsync(BatchSize, now, cancellationToken))
        {
            foreach (var item in batch.Items)
            {
                var reason = PushDeliveryEligibility.CancellationReason(
                    item.Notification,
                    item.Device,
                    item.Preferences);
                if (reason is not null)
                {
                    item.Delivery.Cancel(reason, now);
                    continue;
                }

                var leaseId = Guid.NewGuid();
                item.Delivery.Claim(leaseId, now.Add(PushRetryPolicy.LeaseDuration), now);
                work.Add((
                    item.Delivery,
                    leaseId,
                    new PushMessage(
                        item.Device!.ExpoPushToken,
                        item.Notification!.Id,
                        item.Notification.Title,
                        item.Notification.Body,
                        item.Notification.DeepLink),
                    item.Device.Id));
            }

            await batch.CommitAsync(cancellationToken);
        }

        foreach (var (delivery, leaseId, message, deviceId) in work)
        {
            var result = await sender.SendAsync(message, cancellationToken);
            await RecordSendResultAsync(delivery.Id, leaseId, deviceId, result, cancellationToken);
        }

        return work.Count;
    }

    private async Task RecordSendResultAsync(
        Guid deliveryId,
        Guid leaseId,
        Guid deviceId,
        PushSendResult result,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FindLeasedAsync(deliveryId, leaseId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        var now = clock.GetUtcNow();
        if (result.Accepted && result.TicketId is not null)
        {
            delivery.RecordTicket(leaseId, result.TicketId, now.Add(PushRetryPolicy.FirstReceiptCheckDelay), now);
        }
        else
        {
            if (result.DeviceNotRegistered)
            {
                (await devices.FindAsync(deviceId, cancellationToken))?.Deactivate();
            }

            var delay = PushRetryPolicy.NextSendDelay(delivery.AttemptCount, result.RetryAfter, jitter.NextUnit());
            delivery.Retry(
                leaseId,
                PushRetryPolicy.MaxAttemptsAfterFailure(
                    result.DeviceNotRegistered,
                    result.Retryable,
                    configuration.MaxAttempts),
                now.Add(delay),
                result.ErrorCode ?? NotificationErrorCodes.PushRejected,
                now);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<int> CheckReceiptsAsync(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var work = new List<(Guid DeliveryId, Guid DeviceId, string TicketId)>();
        await using (var batch = await queue.ReserveReceiptChecksAsync(BatchSize, now, cancellationToken))
        {
            foreach (var delivery in batch.Deliveries)
            {
                if (delivery.ExpoTicketId is null ||
                    PushRetryPolicy.IsReceiptExpired(delivery.ExpoTicketCreatedAt, now))
                {
                    delivery.ExpireReceipt(now);
                    continue;
                }

                delivery.RetryReceipt(
                    now.Add(PushRetryPolicy.ReceiptCheckDelay(delivery.ReceiptCheckCount)),
                    NotificationErrorCodes.ReceiptCheckPending);
                work.Add((delivery.Id, delivery.DeviceId, delivery.ExpoTicketId));
            }

            await batch.CommitAsync(cancellationToken);
        }

        foreach (var (deliveryId, deviceId, ticketId) in work)
        {
            var receipt = await sender.GetReceiptAsync(ticketId, cancellationToken);
            await RecordReceiptAsync(deliveryId, deviceId, ticketId, receipt, cancellationToken);
        }

        return work.Count;
    }

    private async Task RecordReceiptAsync(
        Guid deliveryId,
        Guid deviceId,
        string ticketId,
        PushReceiptResult receipt,
        CancellationToken cancellationToken)
    {
        var delivery = await deliveries.FindTicketedAsync(deliveryId, ticketId, cancellationToken);
        if (delivery is null)
        {
            return;
        }

        var now = clock.GetUtcNow();
        if (!receipt.Ready)
        {
            delivery.RetryReceipt(
                now.Add(PushRetryPolicy.ReceiptCheckDelay(delivery.ReceiptCheckCount)),
                receipt.ErrorCode ?? NotificationErrorCodes.ReceiptNotReady);
        }
        else
        {
            if (receipt.DeviceNotRegistered)
            {
                (await devices.FindAsync(deviceId, cancellationToken))?.Deactivate();
            }

            delivery.CompleteReceipt(receipt.Delivered, now, receipt.ErrorCode);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
