namespace Reelix.Domain.Users;

public sealed class UserFavoriteTitle
{
    private UserFavoriteTitle()
    {
    }

    private UserFavoriteTitle(Guid accountId, Guid titleId, string contentType)
    {
        AccountId = accountId;
        TitleId = titleId;
        ContentType = contentType;
    }

    public Guid AccountId { get; private set; }

    public Guid TitleId { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public static UserFavoriteTitle Create(Guid accountId, Guid titleId, string contentType)
    {
        if (accountId == Guid.Empty || titleId == Guid.Empty || string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Informe conta, título e tipo de conteúdo válidos.");
        }

        return new UserFavoriteTitle(accountId, titleId, contentType);
    }
}