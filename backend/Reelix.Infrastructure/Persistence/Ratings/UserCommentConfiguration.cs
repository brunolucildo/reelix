using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Catalog;
using Reelix.Domain.Ratings;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Ratings;

public sealed class UserCommentConfiguration : IEntityTypeConfiguration<UserComment>
{
    public void Configure(EntityTypeBuilder<UserComment> builder)
    {
        builder.ToTable("user_comments", table =>
        {
            table.HasCheckConstraint(
                "CK_user_comments_target",
                "(target_type = 'episode' AND series_id IS NOT NULL) OR " +
                "(target_type IN ('movie', 'series') AND series_id IS NULL)");
            table.HasCheckConstraint(
                "CK_user_comments_content_length",
                "length(btrim(content)) BETWEEN 1 AND 2000");
            table.HasCheckConstraint(
                "CK_user_comments_moderation_version",
                "moderation_version > 0 AND moderation_attempts >= 0");
            table.HasCheckConstraint(
                "CK_user_comments_audience",
                "audience IN ('private', 'public')");
            table.HasCheckConstraint(
                "CK_user_comments_moderation_status",
                "moderation_status IN ('pending', 'published', 'blocked')");
        });
        builder.HasKey(comment => comment.Id);
        builder.Property(comment => comment.Id).HasColumnName("id");
        builder.Property(comment => comment.AccountId).HasColumnName("account_id");
        builder.Property(comment => comment.TargetId).HasColumnName("target_id");
        builder.Property(comment => comment.TargetType)
            .HasColumnName("target_type")
            .HasConversion(
                type => type.ToString().ToLowerInvariant(),
                value => Enum.Parse<ReviewTargetType>(value, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(comment => comment.SeriesId).HasColumnName("series_id");
        builder.Property(comment => comment.Content)
            .HasColumnName("content")
            .HasMaxLength(2000)
            .IsRequired();
        builder.Property(comment => comment.ContainsSpoiler)
            .HasColumnName("contains_spoiler")
            .IsRequired();
        builder.Property(comment => comment.PossibleSpoiler)
            .HasColumnName("possible_spoiler")
            .IsRequired();
        builder.Property(comment => comment.Audience)
            .HasColumnName("audience")
            .HasConversion(
                audience => audience.ToString().ToLowerInvariant(),
                value => Enum.Parse<ReviewAudience>(value, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(comment => comment.AutomatedAnalysisRequested)
            .HasColumnName("automated_analysis_requested")
            .IsRequired();
        builder.Property(comment => comment.ModerationStatus)
            .HasColumnName("moderation_status")
            .HasConversion(
                status => status.ToString().ToLowerInvariant(),
                value => Enum.Parse<CommentModerationStatus>(value, ignoreCase: true))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(comment => comment.ModerationVersion)
            .HasColumnName("moderation_version")
            .IsRequired();
        builder.Property(comment => comment.ModerationAttempts)
            .HasColumnName("moderation_attempts")
            .IsRequired();
        builder.Property(comment => comment.ModerationLeaseId)
            .HasColumnName("moderation_lease_id");
        builder.Property(comment => comment.ModerationLeaseUntil)
            .HasColumnName("moderation_lease_until")
            .HasColumnType("timestamp with time zone");
        builder.Property(comment => comment.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.Property(comment => comment.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(comment => comment.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(comment => comment.TargetId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(comment => comment.SeriesId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(comment => new
        {
            comment.TargetId,
            comment.TargetType,
            comment.Audience,
            comment.ModerationStatus,
            comment.CreatedAt,
            comment.Id,
        })
            .HasDatabaseName("IX_user_comments_target_visibility_created");
        builder.HasIndex(comment => new
        {
            comment.AccountId,
            comment.TargetId,
            comment.CreatedAt,
        })
            .HasDatabaseName("IX_user_comments_account_target_created");
        builder.HasIndex(comment => new
        {
            comment.ModerationStatus,
            comment.ModerationLeaseUntil,
        })
            .HasDatabaseName("IX_user_comments_moderation_queue");
    }
}
