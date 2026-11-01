namespace Reelix.Domain.Users;

public sealed class UserGenrePreference
{
    private UserGenrePreference()
    {
    }

    private UserGenrePreference(Guid accountId, string contentType, int genreId)
    {
        AccountId = accountId;
        ContentType = contentType;
        GenreId = genreId;
    }

    public Guid AccountId { get; private set; }

    public string ContentType { get; private set; } = string.Empty;

    public int GenreId { get; private set; }

    public static UserGenrePreference Create(Guid accountId, string contentType, int genreId)
    {
        if (accountId == Guid.Empty || string.IsNullOrWhiteSpace(contentType) || genreId <= 0)
        {
            throw new ArgumentException("A valid account, content type, and genre are required.");
        }

        return new UserGenrePreference(accountId, contentType, genreId);
    }
}