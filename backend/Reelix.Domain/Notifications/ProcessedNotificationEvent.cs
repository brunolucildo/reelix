namespace Reelix.Domain.Notifications;

public sealed class ProcessedNotificationEvent
{
    private ProcessedNotificationEvent()
    {
    }

    private ProcessedNotificationEvent(Guid eventId, string outcome, DateTimeOffset processedAt)
    {
        EventId = eventId;
        Outcome = outcome;
        ProcessedAt = processedAt;
    }

    public Guid EventId { get; private set; }

    public string Outcome { get; private set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; private set; }

    public static ProcessedNotificationEvent Create(Guid eventId, string outcome)
    {
        if (eventId == Guid.Empty || outcome is not ("created" or "disabled" or "obsolete"))
        {
            throw new ArgumentException("O recibo de evento de notificação é inválido.");
        }

        return new ProcessedNotificationEvent(eventId, outcome, DateTimeOffset.UtcNow);
    }
}
