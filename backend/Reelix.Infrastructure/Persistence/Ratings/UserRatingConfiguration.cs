using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Catalog;
using Reelix.Domain.Ratings;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Ratings;

public sealed class UserRatingConfiguration : IEntityTypeConfiguration<UserRating>
{
    public void Configure(EntityTypeBuilder<UserRating> builder)
    {
        builder.ToTable("user_ratings", table =>
        {
            table.HasCheckConstraint(
                "CK_user_ratings_score",
                "score >= 0 AND score <= 5 AND score * 2 = trunc(score * 2)");
            table.HasCheckConstraint(
                "CK_user_ratings_target",
                "(target_type = 'episode' AND series_id IS NOT NULL) OR " +
                "(target_type IN ('movie', 'series') AND series_id IS NULL)");
            table.HasCheckConstraint(
                "CK_user_ratings_audience",
                "audience IN ('private', 'public')");
        });
        builder.HasKey(rating => rating.Id);
        builder.Property(rating => rating.Id).HasColumnName("id");
        builder.Property(rating => rating.AccountId).HasColumnName("account_id");
        builder.Property(rating => rating.TargetId).HasColumnName("target_id");
        builder.Property(rating => rating.TargetType)
            .HasColumnName("target_type")
            .HasConversion(
                type => type.ToString().ToLowerInvariant(),
                value => Enum.Parse<ReviewTargetType>(value, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(rating => rating.SeriesId).HasColumnName("series_id");
        builder.Property(rating => rating.Value)
            .HasColumnName("score")
            .HasColumnType("numeric(2,1)")
            .IsRequired();
        builder.Property(rating => rating.Audience)
            .HasColumnName("audience")
            .HasConversion(
                audience => audience.ToString().ToLowerInvariant(),
                value => Enum.Parse<ReviewAudience>(value, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(rating => rating.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(rating => rating.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(rating => rating.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(rating => rating.TargetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(rating => rating.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rating => new { rating.AccountId, rating.TargetId })
            .HasDatabaseName("UX_user_ratings_account_target")
            .IsUnique();
        builder.HasIndex(rating => new
        {
            rating.TargetId,
            rating.TargetType,
            rating.Audience,
        })
            .HasDatabaseName("IX_user_ratings_target_audience");
    }
}
