namespace Reelix.Domain.Users;

public sealed class UserProfile
{
    private UserProfile()
    {
    }

    private UserProfile(Guid accountId)
    {
        AccountId = accountId;
    }

    public Guid AccountId { get; private set; }

    public string? DisplayName { get; private set; }

    public string? Biography { get; private set; }

    public bool ProfileIsPublic { get; private set; }

    public bool FavoritesArePublic { get; private set; }

    public bool WatchHistoryIsPublic { get; private set; }

    public static UserProfile Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe um identificador de conta válido.", nameof(accountId));
        }

        return new UserProfile(accountId);
    }

    public void Update(
        string? displayName,
        string? biography,
        bool profileIsPublic,
        bool favoritesArePublic,
        bool watchHistoryIsPublic)
    {
        var normalizedName = NormalizeOptional(displayName);
        var normalizedBiography = NormalizeOptional(biography);
        if (normalizedName is { Length: > 50 })
        {
            throw new ArgumentException("O nome de exibição pode ter no máximo 50 caracteres.", nameof(displayName));
        }

        if (normalizedBiography is { Length: > 280 })
        {
            throw new ArgumentException("A biografia pode ter no máximo 280 caracteres.", nameof(biography));
        }

        DisplayName = normalizedName;
        Biography = normalizedBiography;
        ProfileIsPublic = profileIsPublic;
        FavoritesArePublic = favoritesArePublic;
        WatchHistoryIsPublic = watchHistoryIsPublic;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}