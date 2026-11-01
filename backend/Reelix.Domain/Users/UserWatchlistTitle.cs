namespace Reelix.Domain.Users;

public sealed class UserWatchlistTitle
{
    private UserWatchlistTitle()
    {
    }

    private UserWatchlistTitle(Guid accountId, Guid titleId, string contentType, DateTimeOffset addedAt)
    {
        AccountId = accountId;
        TitleId = titleId;
        ContentType = contentType;
        AddedAt = addedAt;
    }

    public Guid AccountId { get; private set; }

    public Guid TitleId { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public DateTimeOffset AddedAt { get; private set; }

    public static UserWatchlistTitle Create(
        Guid accountId,
        Guid titleId,
        string contentType,
        DateTimeOffset? addedAt = null)
    {
        if (accountId == Guid.Empty || titleId == Guid.Empty ||
            contentType is not ("movie" or "series"))
        {
            throw new ArgumentException("Informe conta, título e tipo de conteúdo válidos.");
        }

        return new UserWatchlistTitle(accountId, titleId, contentType, addedAt ?? DateTimeOffset.UtcNow);
    }
}
