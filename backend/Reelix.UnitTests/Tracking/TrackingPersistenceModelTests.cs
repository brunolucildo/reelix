using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Tracking;
using Reelix.Infrastructure.Persistence.Tracking;

namespace Reelix.UnitTests.Tracking;

public sealed class TrackingPersistenceModelTests
{
    [Fact]
    public void TrackingMapping_UsesDurableUniquenessAndNonCascadingCatalogReferences()
    {
        using var context = new TrackingModelContext(
            new DbContextOptionsBuilder<TrackingModelContext>()
                .UseNpgsql("Host=localhost;Database=reelix;Username=reelix;Password=unused")
                .Options);
        var entity = context.Model.FindEntityType(typeof(TrackingRecord));

        Assert.NotNull(entity);
        Assert.Equal("tracking_records", entity.GetTableName());
        var uniqueIndex = Assert.Single(entity.GetIndexes(), index =>
            index.GetDatabaseName() == "UX_tracking_records_account_target");
        Assert.True(uniqueIndex.IsUnique);
        Assert.Contains(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "IX_tracking_records_account_watched_at");
        Assert.Contains(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "IX_tracking_records_account_type_watched_at");
        Assert.Contains(
            entity.GetIndexes(),
            index => index.GetDatabaseName() == "IX_tracking_records_account_series_target");
        Assert.Contains(
            "item_type = 'movie'",
            context.TrackingCheckConstraintSql,
            StringComparison.Ordinal);
        Assert.Contains(
            "series_id IS NOT NULL",
            context.TrackingCheckConstraintSql,
            StringComparison.Ordinal);
        Assert.Contains(
            entity.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Any(property => property.Name == "TargetId") &&
                foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Contains(
            entity.GetForeignKeys(),
            foreignKey => foreignKey.Properties.Any(property => property.Name == "SeriesId") &&
                foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    private sealed class TrackingModelContext(DbContextOptions<TrackingModelContext> options)
        : DbContext(options)
    {
        public string TrackingCheckConstraintSql { get; private set; } = string.Empty;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TrackingRecordConfiguration());
            TrackingCheckConstraintSql = modelBuilder.Model
                .FindEntityType(typeof(TrackingRecord))!
                .GetCheckConstraints()
                .Single(constraint => constraint.Name == "CK_tracking_records_target_type")
                .Sql;
        }
    }
}
