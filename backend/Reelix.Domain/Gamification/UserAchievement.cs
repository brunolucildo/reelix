namespace Reelix.Domain.Gamification;

public enum UserAchievementStatus
{
    Earned,
    Revoked,
}

public sealed class UserAchievement
{
    private UserAchievement()
    {
    }

    private UserAchievement(Guid accountId, Guid definitionId, DateTimeOffset now)
    {
        AccountId = accountId;
        DefinitionId = definitionId;
        Status = UserAchievementStatus.Earned;
        FirstEarnedAt = now;
        LastEarnedAt = now;
        UpdatedAt = now;
    }

    public Guid AccountId { get; private set; }

    public Guid DefinitionId { get; private set; }

    public UserAchievementStatus Status { get; private set; }

    public DateTimeOffset FirstEarnedAt { get; private set; }

    public DateTimeOffset LastEarnedAt { get; private set; }

    public DateTimeOffset? LastRevokedAt { get; private set; }

    public DateTimeOffset? ReactivatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static UserAchievement Create(Guid accountId, Guid definitionId, DateTimeOffset now)
    {
        if (accountId == Guid.Empty || definitionId == Guid.Empty)
        {
            throw new ArgumentException("Informe conta e definição de conquista válidas.");
        }

        return new UserAchievement(accountId, definitionId, now);
    }

    public void Earn(DateTimeOffset now)
    {
        if (Status == UserAchievementStatus.Earned)
        {
            return;
        }

        Status = UserAchievementStatus.Earned;
        LastEarnedAt = now;
        ReactivatedAt = now;
        UpdatedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (Status == UserAchievementStatus.Revoked)
        {
            return;
        }

        Status = UserAchievementStatus.Revoked;
        LastRevokedAt = now;
        UpdatedAt = now;
    }
}
