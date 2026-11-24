using Microsoft.EntityFrameworkCore;
using Reelix.Application.Gamification;
using Reelix.Domain.Gamification;

namespace Reelix.Infrastructure.Persistence.Gamification;

/// <summary>The write adapter of <c>user_achievements</c>.</summary>
public sealed class UserAchievementStore(ReelixDbContext dbContext) : IUserAchievements
{
    public async Task<IReadOnlyList<UserAchievement>> ListAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await dbContext.UserAchievements
            .Where(achievement => achievement.AccountId == accountId)
            .ToArrayAsync(cancellationToken);

    public async Task SaveAsync(
        IReadOnlyCollection<UserAchievement> granted,
        CancellationToken cancellationToken)
    {
        dbContext.UserAchievements.AddRange(granted);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
