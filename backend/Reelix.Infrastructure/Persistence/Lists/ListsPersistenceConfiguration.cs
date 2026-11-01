using Microsoft.EntityFrameworkCore;

namespace Reelix.Infrastructure.Persistence.Lists;

public static class ListsPersistenceConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserWatchlistTitleConfiguration());
        modelBuilder.ApplyConfiguration(new UserCustomListConfiguration());
        modelBuilder.ApplyConfiguration(new UserCustomListTitleConfiguration());
    }
}
