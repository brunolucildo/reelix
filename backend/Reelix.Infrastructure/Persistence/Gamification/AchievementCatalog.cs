using Microsoft.EntityFrameworkCore;
using Reelix.Application.Gamification;
using Reelix.Domain.Gamification;

namespace Reelix.Infrastructure.Persistence.Gamification;

public sealed class AchievementCatalog(ReelixDbContext dbContext) : IAchievementCatalog
{
    public async Task<IReadOnlyList<AchievementDefinition>> ListActiveAsync(
        CancellationToken cancellationToken) =>
        await dbContext.AchievementDefinitions
            .Where(definition => definition.IsActive)
            .OrderBy(definition => definition.Code)
            .ToArrayAsync(cancellationToken);
}
