namespace Reelix.Domain.Gamification;

public sealed class GamificationPreferences
{
    private GamificationPreferences()
    {
    }

    private GamificationPreferences(Guid accountId, DateTimeOffset now)
    {
        AccountId = accountId;
        BadgesPublic = true;
        UpdatedAt = now;
    }

    public Guid AccountId { get; private set; }

    public bool BadgesPublic { get; private set; }

    public bool RankingEnabled { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static GamificationPreferences Create(Guid accountId, DateTimeOffset now)
    {
        if (accountId == Guid.Empty)
        {
            throw new ArgumentException("Informe uma conta válida.", nameof(accountId));
        }

        return new GamificationPreferences(accountId, now);
    }

    public void Update(bool badgesPublic, bool rankingEnabled, DateTimeOffset now)
    {
        BadgesPublic = badgesPublic;
        RankingEnabled = rankingEnabled;
        UpdatedAt = now;
    }
}
