namespace Reelix.Domain.Gamification;

public sealed class GamificationPreferences
{
    private GamificationPreferences()
    {
    }

    private GamificationPreferences(Guid accountId)
    {
        AccountId = accountId;
        BadgesPublic = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid AccountId { get; private set; }

    public bool BadgesPublic { get; private set; }

    public bool RankingEnabled { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static GamificationPreferences Create(Guid accountId)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta válida.", nameof(accountId));
        }

        return new GamificationPreferences(accountId);
    }

    public void Update(bool badgesPublic, bool rankingEnabled)
    {
        BadgesPublic = badgesPublic;
        RankingEnabled = rankingEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
