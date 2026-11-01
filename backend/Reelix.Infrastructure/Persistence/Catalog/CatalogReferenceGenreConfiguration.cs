using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Catalog;

namespace Reelix.Infrastructure.Persistence.Catalog;

public sealed class CatalogReferenceGenreConfiguration
    : IEntityTypeConfiguration<CatalogReferenceGenre>
{
    public void Configure(EntityTypeBuilder<CatalogReferenceGenre> builder)
    {
        builder.ToTable("catalog_reference_genres", table =>
            table.HasCheckConstraint(
                "CK_catalog_reference_genres_positive_id",
                "genre_id > 0"));
        builder.HasKey(genre => new { genre.CatalogReferenceId, genre.GenreId });
        builder.Property(genre => genre.CatalogReferenceId).HasColumnName("catalog_reference_id");
        builder.Property(genre => genre.GenreId).HasColumnName("genre_id");
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(genre => genre.CatalogReferenceId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(genre => genre.GenreId)
            .HasDatabaseName("IX_catalog_reference_genres_genre_id");
    }
}
