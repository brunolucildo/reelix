using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Reelix.Domain.Catalog;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence.Lists;

namespace Reelix.UnitTests.Lists;

public sealed class ListsPersistenceConfigurationTests
{
    [Fact]
    public void Apply_ProtectsWatchlistAndCustomListTitleUniqueness()
    {
        var modelBuilder = new ModelBuilder();
        ListsPersistenceConfiguration.Apply(modelBuilder);

        AssertCompositePrimaryKey<UserWatchlistTitle>(modelBuilder, nameof(UserWatchlistTitle.AccountId), nameof(UserWatchlistTitle.TitleId));
        AssertCompositePrimaryKey<UserCustomListTitle>(modelBuilder, nameof(UserCustomListTitle.ListId), nameof(UserCustomListTitle.TitleId));
    }

    [Fact]
    public void Apply_CascadesOwnedListItemsButPreservesCatalogReferences()
    {
        var modelBuilder = new ModelBuilder();
        ListsPersistenceConfiguration.Apply(modelBuilder);

        var listItem = modelBuilder.Model.FindEntityType(typeof(UserCustomListTitle))!;
        var listForeignKey = listItem.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(UserCustomList));
        var catalogForeignKey = listItem.GetForeignKeys()
            .Single(key => key.PrincipalEntityType.ClrType == typeof(ExternalCatalogReference));

        Assert.Equal(DeleteBehavior.Cascade, listForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Restrict, catalogForeignKey.DeleteBehavior);
    }

    private static void AssertCompositePrimaryKey<TEntity>(ModelBuilder modelBuilder, params string[] properties)
    {
        var entity = modelBuilder.Model.FindEntityType(typeof(TEntity))!;
        var key = entity.FindPrimaryKey()!;

        Assert.Equal(properties, key.Properties.Select(property => property.Name));
    }
}
