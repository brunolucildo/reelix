using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Catalog;
using Reelix.Domain.Tracking;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Tracking;

public sealed class TrackingRecordConfiguration : IEntityTypeConfiguration<TrackingRecord>
{
    public void Configure(EntityTypeBuilder<TrackingRecord> entity)
    {
        entity.ToTable("tracking_records", table =>
            table.HasCheckConstraint(
                "CK_tracking_records_target_type",
                "(item_type = 'movie' AND series_id IS NULL) OR " +
                "(item_type = 'episode' AND series_id IS NOT NULL)"));
        entity.HasKey(record => record.Id);
        entity.Property(record => record.Id).HasColumnName("id");
        entity.Property(record => record.AccountId).HasColumnName("account_id").IsRequired();
        entity.Property(record => record.TargetId).HasColumnName("target_id").IsRequired();
        entity.Property(record => record.ItemType)
            .HasColumnName("item_type")
            .HasMaxLength(20)
            .HasConversion(
                value => value.ToString().ToLowerInvariant(),
                value => Enum.Parse<TrackingItemType>(value, ignoreCase: true))
            .IsRequired();
        entity.Property(record => record.SeriesId).HasColumnName("series_id");
        entity.Property(record => record.WatchedAt)
            .HasColumnName("watched_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        entity.Property(record => record.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        entity.HasIndex(record => new { record.AccountId, record.TargetId })
            .HasDatabaseName("UX_tracking_records_account_target")
            .IsUnique();
        entity.HasIndex(record => new { record.AccountId, record.WatchedAt, record.Id })
            .HasDatabaseName("IX_tracking_records_account_watched_at");
        entity.HasIndex(record => new
            {
                record.AccountId,
                record.ItemType,
                record.WatchedAt,
                record.Id,
            })
            .HasDatabaseName("IX_tracking_records_account_type_watched_at");
        entity.HasIndex(record => new { record.AccountId, record.SeriesId, record.TargetId })
            .HasDatabaseName("IX_tracking_records_account_series_target");
        entity.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(record => record.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(record => record.TargetId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(record => record.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
