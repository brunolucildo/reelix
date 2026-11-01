using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Gamification;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Gamification;

public static class GamificationPersistenceConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AchievementDefinition>(entity =>
        {
            entity.ToTable("achievement_definitions", table =>
            {
                table.HasCheckConstraint(
                    "CK_achievement_definitions_threshold_version",
                    "threshold > 0 AND version > 0");
                table.HasCheckConstraint(
                    "CK_achievement_definitions_rule_type",
                    "rule_type IN ('MovieCount', 'EpisodeCount', 'SeriesCount', 'MovieGenreCount')");
            });
            entity.HasKey(definition => definition.Id);
            entity.Property(definition => definition.Id).HasColumnName("id");
            entity.Property(definition => definition.Code)
                .HasColumnName("code")
                .HasMaxLength(60)
                .IsRequired();
            entity.Property(definition => definition.Title)
                .HasColumnName("title")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(definition => definition.Description)
                .HasColumnName("description")
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(definition => definition.RuleType)
                .HasColumnName("rule_type")
                .HasConversion<string>()
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(definition => definition.Threshold)
                .HasColumnName("threshold")
                .IsRequired();
            entity.Property(definition => definition.Version)
                .HasColumnName("version")
                .IsRequired();
            entity.Property(definition => definition.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true);
            entity.HasIndex(definition => definition.Code)
                .IsUnique()
                .HasDatabaseName("UX_achievement_definitions_code");
            entity.HasData(AchievementDefinition.InitialCatalog);
        });

        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.ToTable("user_achievements", table =>
                table.HasCheckConstraint(
                    "CK_user_achievements_status",
                    "status IN ('Earned', 'Revoked')"));
            entity.HasKey(achievement => new { achievement.AccountId, achievement.DefinitionId });
            entity.Property(achievement => achievement.AccountId).HasColumnName("account_id");
            entity.Property(achievement => achievement.DefinitionId).HasColumnName("definition_id");
            entity.Property(achievement => achievement.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(achievement => achievement.FirstEarnedAt)
                .HasColumnName("first_earned_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(achievement => achievement.LastEarnedAt)
                .HasColumnName("last_earned_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.Property(achievement => achievement.LastRevokedAt)
                .HasColumnName("last_revoked_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(achievement => achievement.ReactivatedAt)
                .HasColumnName("reactivated_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(achievement => achievement.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(achievement => achievement.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<AchievementDefinition>()
                .WithMany()
                .HasForeignKey(achievement => achievement.DefinitionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<GamificationPreferences>(entity =>
        {
            entity.ToTable("gamification_preferences");
            entity.HasKey(preferences => preferences.AccountId);
            entity.Property(preferences => preferences.AccountId).HasColumnName("account_id");
            entity.Property(preferences => preferences.BadgesPublic)
                .HasColumnName("badges_public")
                .HasDefaultValue(true)
                .IsRequired();
            entity.Property(preferences => preferences.RankingEnabled)
                .HasColumnName("ranking_enabled")
                .HasDefaultValue(false)
                .IsRequired();
            entity.Property(preferences => preferences.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasOne<UserAccount>()
                .WithOne()
                .HasForeignKey<GamificationPreferences>(preferences => preferences.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
