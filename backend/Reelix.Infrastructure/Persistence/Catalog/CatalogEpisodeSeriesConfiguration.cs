using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Catalog;

namespace Reelix.Infrastructure.Persistence.Catalog;

public sealed class CatalogEpisodeSeriesConfiguration
    : IEntityTypeConfiguration<CatalogEpisodeSeries>
{
    public void Configure(EntityTypeBuilder<CatalogEpisodeSeries> builder)
    {
        builder.ToTable("catalog_episode_series", table =>
            table.HasCheckConstraint(
                "CK_catalog_episode_series_distinct_ids",
                "episode_id <> series_id"));
        builder.HasKey(link => link.EpisodeId);
        builder.Property(link => link.EpisodeId).HasColumnName("episode_id");
        builder.Property(link => link.SeriesId).HasColumnName("series_id");
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(link => link.EpisodeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(link => link.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => link.SeriesId)
            .HasDatabaseName("IX_catalog_episode_series_series_id");
    }
}
