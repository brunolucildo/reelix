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

    public bool IsDiscoverable { get; private set; }

    public bool WatchHistorySharedWithFriends { get; private set; }

    public DateTimeOffset? WatchHistorySharedWithFriendsSince { get; private set; }

    public bool FavoritesSharedWithFriends { get; private set; }

    public bool RatingsSharedWithFriends { get; private set; }

    public DateTimeOffset? RatingsSharedWithFriendsSince { get; private set; }
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
        bool watchHistoryIsPublic,
        bool isDiscoverable = false,
        bool watchHistorySharedWithFriends = false,
        bool favoritesSharedWithFriends = false,
        bool ratingsSharedWithFriends = false)
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

        var now = DateTimeOffset.UtcNow;
        if (watchHistorySharedWithFriends != WatchHistorySharedWithFriends)
        {
            WatchHistorySharedWithFriendsSince = watchHistorySharedWithFriends ? now : null;
        }

        if (ratingsSharedWithFriends != RatingsSharedWithFriends)
        {
            RatingsSharedWithFriendsSince = ratingsSharedWithFriends ? now : null;
        }

        DisplayName = normalizedName;
        Biography = normalizedBiography;
        ProfileIsPublic = profileIsPublic;
        FavoritesArePublic = favoritesArePublic;
        WatchHistoryIsPublic = watchHistoryIsPublic;
        IsDiscoverable = isDiscoverable;
        WatchHistorySharedWithFriends = watchHistorySharedWithFriends;
        FavoritesSharedWithFriends = favoritesSharedWithFriends;
        RatingsSharedWithFriends = ratingsSharedWithFriends;
    }

    private static string? NormalizeOptional(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}