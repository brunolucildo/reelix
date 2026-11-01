namespace Reelix.Domain.Social;

public enum FriendRelationshipStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled,
    Removed,
}

public sealed class FriendRelationship
{
    private FriendRelationship()
    {
    }

    private FriendRelationship(
        Guid id,
        Guid firstAccountId,
        Guid secondAccountId,
        Guid requesterAccountId,
        Guid addresseeAccountId,
        DateTimeOffset createdAt)
    {
        Id = id;
        FirstAccountId = firstAccountId;
        SecondAccountId = secondAccountId;
        RequesterAccountId = requesterAccountId;
        AddresseeAccountId = addresseeAccountId;
        Status = FriendRelationshipStatus.Pending;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid FirstAccountId { get; private set; }

    public Guid SecondAccountId { get; private set; }

    public Guid RequesterAccountId { get; private set; }

    public Guid AddresseeAccountId { get; private set; }

    public FriendRelationshipStatus Status { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static FriendRelationship Create(Guid requesterAccountId, Guid addresseeAccountId)
    {
        ValidateParticipants(requesterAccountId, addresseeAccountId);
        var (first, second) = Order(requesterAccountId, addresseeAccountId);
        return new FriendRelationship(
            Guid.NewGuid(),
            first,
            second,
            requesterAccountId,
            addresseeAccountId,
            DateTimeOffset.UtcNow);
    }

    public void StartRequest(Guid requesterAccountId, Guid addresseeAccountId)
    {
        ValidateParticipants(requesterAccountId, addresseeAccountId);
        if (Order(requesterAccountId, addresseeAccountId) != (FirstAccountId, SecondAccountId))
        {
            throw new ArgumentException("A solicitação deve pertencer ao mesmo par de contas.");
        }

        if (Status is FriendRelationshipStatus.Pending or FriendRelationshipStatus.Accepted)
        {
            throw new InvalidOperationException("Já existe uma relação ativa entre essas contas.");
        }

        RequesterAccountId = requesterAccountId;
        AddresseeAccountId = addresseeAccountId;
        Status = FriendRelationshipStatus.Pending;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Accept(Guid actorAccountId)
    {
        RequirePendingRecipient(actorAccountId);
        Status = FriendRelationshipStatus.Accepted;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Decline(Guid actorAccountId)
    {
        RequirePendingRecipient(actorAccountId);
        Status = FriendRelationshipStatus.Declined;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Cancel(Guid actorAccountId)
    {
        if (Status != FriendRelationshipStatus.Pending || RequesterAccountId != actorAccountId)
        {
            throw new InvalidOperationException("Somente quem enviou a solicitação pode cancelá-la.");
        }

        Status = FriendRelationshipStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Remove(Guid actorAccountId)
    {
        if (Status != FriendRelationshipStatus.Accepted ||
            actorAccountId != FirstAccountId && actorAccountId != SecondAccountId)
        {
            throw new InvalidOperationException("Somente um dos amigos pode remover esta amizade.");
        }

        Status = FriendRelationshipStatus.Removed;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public bool Includes(Guid accountId) =>
        accountId == FirstAccountId || accountId == SecondAccountId;

    public Guid OtherAccount(Guid accountId) =>
        accountId == FirstAccountId
            ? SecondAccountId
            : accountId == SecondAccountId
                ? FirstAccountId
                : throw new ArgumentException("A conta não participa desta relação.", nameof(accountId));

    private void RequirePendingRecipient(Guid actorAccountId)
    {
        if (Status != FriendRelationshipStatus.Pending || AddresseeAccountId != actorAccountId)
        {
            throw new InvalidOperationException("Somente o destinatário pode responder a uma solicitação pendente.");
        }
    }

    private static void ValidateParticipants(Guid requesterAccountId, Guid addresseeAccountId)
    {
        if (requesterAccountId == Guid.Empty || addresseeAccountId == Guid.Empty)
        {
            throw new ArgumentException("Informe duas contas válidas.");
        }

        if (requesterAccountId == addresseeAccountId)
        {
            throw new ArgumentException("Não é possível enviar uma solicitação para si mesmo.");
        }
    }

    private static (Guid First, Guid Second) Order(Guid first, Guid second) =>
        first.CompareTo(second) < 0 ? (first, second) : (second, first);
}
