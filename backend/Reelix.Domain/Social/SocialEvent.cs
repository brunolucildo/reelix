namespace Reelix.Domain.Social;

public sealed class SocialEvent
{
    private SocialEvent()
    {
    }

    private SocialEvent(
        Guid id,
        string eventType,
        Guid actorAccountId,
        Guid recipientAccountId,
        DateTimeOffset occurredAt)
    {
        Id = id;
        EventType = eventType;
        EventVersion = 1;
        ActorAccountId = actorAccountId;
        RecipientAccountId = recipientAccountId;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }

    public string EventType { get; private set; } = string.Empty;

    public int EventVersion { get; private set; }

    public Guid ActorAccountId { get; private set; }

    public Guid RecipientAccountId { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public static SocialEvent Create(
        string eventType,
        Guid actorAccountId,
        Guid recipientAccountId)
    {
        if (eventType is not ("friend.requested" or "friend.accepted"))
        {
            throw new ArgumentOutOfRangeException(nameof(eventType), "O evento social é inválido.");
        }

        if (actorAccountId == Guid.Empty || recipientAccountId == Guid.Empty ||
            actorAccountId == recipientAccountId)
        {
            throw new ArgumentException("Informe contas distintas e válidas.");
        }

        return new SocialEvent(
            Guid.NewGuid(),
            eventType,
            actorAccountId,
            recipientAccountId,
            DateTimeOffset.UtcNow);
    }
}
