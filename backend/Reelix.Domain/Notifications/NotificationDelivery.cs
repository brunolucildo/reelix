namespace Reelix.Domain.Notifications;

public enum NotificationDeliveryStatus
{
    Pending,
    Ticketed,
    Completed,
    Failed,
    Cancelled,
}

public sealed class NotificationDelivery
{
    private NotificationDelivery()
    {
    }

    private NotificationDelivery(Guid notificationId, Guid deviceId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        NotificationId = notificationId;
        DeviceId = deviceId;
        Status = NotificationDeliveryStatus.Pending;
        AttemptCount = 0;
        NextAttemptAt = now;
    }

    public Guid Id { get; private set; }

    public Guid NotificationId { get; private set; }

    public Guid DeviceId { get; private set; }

    public NotificationDeliveryStatus Status { get; private set; }

    public int AttemptCount { get; private set; }

    public DateTimeOffset NextAttemptAt { get; private set; }

    public Guid? LeaseId { get; private set; }

    public DateTimeOffset? LeaseUntil { get; private set; }

    public string? ExpoTicketId { get; private set; }

    public DateTimeOffset? ExpoTicketCreatedAt { get; private set; }

    public int ReceiptCheckCount { get; private set; }

    public DateTimeOffset? ReceiptCheckAt { get; private set; }

    public string? LastErrorCode { get; private set; }

    public DateTimeOffset? CompletedAt { get; private set; }

    public static NotificationDelivery Create(Guid notificationId, Guid deviceId, DateTimeOffset now)
    {
        if (notificationId == Guid.Empty || deviceId == Guid.Empty)
        {
            throw new ArgumentException("Informe notificação e dispositivo válidos.");
        }

        return new NotificationDelivery(notificationId, deviceId, now);
    }

    public void Claim(Guid leaseId, DateTimeOffset leaseUntil, DateTimeOffset now)
    {
        if (Status != NotificationDeliveryStatus.Pending ||
            LeaseUntil > now ||
            leaseId == Guid.Empty ||
            leaseUntil <= now)
        {
            throw new InvalidOperationException("A entrega não está disponível para processamento.");
        }

        LeaseId = leaseId;
        LeaseUntil = leaseUntil;
        AttemptCount++;
    }

    public void RecordTicket(
        Guid leaseId,
        string ticketId,
        DateTimeOffset receiptCheckAt,
        DateTimeOffset now)
    {
        RequireLease(leaseId);
        if (string.IsNullOrWhiteSpace(ticketId) || receiptCheckAt <= now)
        {
            throw new ArgumentException("O ticket Expo é inválido.");
        }

        Status = NotificationDeliveryStatus.Ticketed;
        ExpoTicketId = ticketId;
        ExpoTicketCreatedAt = now;
        ReceiptCheckCount = 0;
        ReceiptCheckAt = receiptCheckAt;
        ClearLease();
    }

    public void Retry(
        Guid leaseId,
        int maxAttempts,
        DateTimeOffset nextAttemptAt,
        string errorCode,
        DateTimeOffset now)
    {
        RequireLease(leaseId);
        LastErrorCode = NormalizeErrorCode(errorCode);
        if (AttemptCount >= maxAttempts)
        {
            Status = NotificationDeliveryStatus.Failed;
            CompletedAt = now;
        }
        else
        {
            NextAttemptAt = nextAttemptAt;
        }

        ClearLease();
    }

    public void CompleteReceipt(bool delivered, DateTimeOffset now, string? errorCode = null)
    {
        if (Status != NotificationDeliveryStatus.Ticketed)
        {
            throw new InvalidOperationException("A entrega não está aguardando um recibo Expo.");
        }

        Status = delivered ? NotificationDeliveryStatus.Completed : NotificationDeliveryStatus.Failed;
        LastErrorCode = errorCode is null ? null : NormalizeErrorCode(errorCode);
        CompletedAt = now;
        ReceiptCheckAt = null;
    }

    public void RetryReceipt(DateTimeOffset nextCheckAt, string errorCode)
    {
        if (Status != NotificationDeliveryStatus.Ticketed)
        {
            throw new InvalidOperationException("A entrega não está aguardando um recibo Expo.");
        }

        LastErrorCode = NormalizeErrorCode(errorCode);
        ReceiptCheckAt = nextCheckAt;
        ReceiptCheckCount++;
    }

    public void ExpireReceipt(DateTimeOffset now)
    {
        if (Status != NotificationDeliveryStatus.Ticketed)
        {
            throw new InvalidOperationException("A entrega não está aguardando um recibo Expo.");
        }

        Status = NotificationDeliveryStatus.Failed;
        LastErrorCode = "receipt_expired";
        CompletedAt = now;
        ReceiptCheckAt = null;
    }

    public void Cancel(string errorCode, DateTimeOffset now)
    {
        if (Status is NotificationDeliveryStatus.Completed or NotificationDeliveryStatus.Failed)
        {
            return;
        }

        Status = NotificationDeliveryStatus.Cancelled;
        LastErrorCode = NormalizeErrorCode(errorCode);
        CompletedAt = now;
        ClearLease();
    }

    private void RequireLease(Guid leaseId)
    {
        if (LeaseId != leaseId)
        {
            throw new InvalidOperationException("A entrega não pertence a este processamento.");
        }
    }

    private void ClearLease()
    {
        LeaseId = null;
        LeaseUntil = null;
    }

    private static string NormalizeErrorCode(string errorCode)
    {
        if (string.IsNullOrWhiteSpace(errorCode) || errorCode.Length > 80)
        {
            throw new ArgumentException("O código de erro é inválido.", nameof(errorCode));
        }

        return errorCode;
    }
}
