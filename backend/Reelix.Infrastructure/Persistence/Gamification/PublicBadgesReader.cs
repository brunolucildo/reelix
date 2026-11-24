using Microsoft.EntityFrameworkCore;
using Reelix.Application.Gamification;
using Reelix.Domain.Gamification;

namespace Reelix.Infrastructure.Persistence.Gamification;

public sealed class PublicBadgesReader(ReelixDbContext dbContext) : IPublicBadgesReader
{
    public async Task<IReadOnlyList<PublicBadge>> ListEarnedAsync(
        Guid accountId,
        CancellationToken cancellationToken) =>
        await (
                from achievement in dbContext.UserAchievements
                join definition in dbContext.AchievementDefinitions
                    on achievement.DefinitionId equals definition.Id
                where achievement.AccountId == accountId &&
                      achievement.Status == UserAchievementStatus.Earned &&
                      definition.IsActive
                orderby achievement.FirstEarnedAt, definition.Code
                select new PublicBadge(
                    definition.Code,
                    definition.Title,
                    definition.Description,
                    achievement.FirstEarnedAt))
            .ToArrayAsync(cancellationToken);
}
