using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Notifications;

namespace Reelix.Infrastructure.Persistence.Notifications;

public static class NotificationsPersistenceConfiguration
{
    public static void Apply(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationPreferences>(entity =>
        {
            entity.ToTable("notification_preferences");
            entity.HasKey(preferences => preferences.AccountId);
            entity.Property(preferences => preferences.AccountId).HasColumnName("account_id");
            entity.Property(preferences => preferences.SocialEnabled).HasColumnName("social_enabled");
            entity.Property(preferences => preferences.NewEpisodeEnabled).HasColumnName("new_episode_enabled");
            entity.Property(preferences => preferences.PremiereEnabled).HasColumnName("premiere_enabled");
            entity.HasOne<Domain.Users.UserAccount>()
                .WithOne()
                .HasForeignKey<NotificationPreferences>(preferences => preferences.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationDevice>(entity =>
        {
            entity.ToTable("notification_devices", table =>
                table.HasCheckConstraint(
                    "CK_notification_devices_platform",
                    "platform IN ('Ios', 'Android')"));
            entity.HasKey(device => device.Id);
            entity.Property(device => device.Id).HasColumnName("id");
            entity.Property(device => device.AccountId).HasColumnName("account_id");
            entity.Property(device => device.InstallationId).HasColumnName("installation_id");
            entity.Property(device => device.ExpoPushToken)
                .HasColumnName("expo_push_token")
                .HasMaxLength(255)
                .IsRequired();
            entity.Property(device => device.Platform)
                .HasColumnName("platform")
                .HasConversion<string>()
                .HasMaxLength(10)
                .IsRequired();
            entity.Property(device => device.ProjectId).HasColumnName("project_id");
            entity.Property(device => device.IsActive).HasColumnName("is_active");
            entity.Property(device => device.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(device => device.LastRegisteredAt)
                .HasColumnName("last_registered_at")
                .HasColumnType("timestamp with time zone");
            entity.HasIndex(device => device.InstallationId)
                .IsUnique()
                .HasDatabaseName("UX_notification_devices_installation");
            entity.HasIndex(device => device.ExpoPushToken)
                .IsUnique()
                .HasFilter("is_active")
                .HasDatabaseName("UX_notification_devices_active_token");
            entity.HasIndex(device => new { device.AccountId, device.IsActive })
                .HasDatabaseName("IX_notification_devices_account_active");
            entity.HasOne<Domain.Users.UserAccount>()
                .WithMany()
                .HasForeignKey(device => device.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserNotification>(entity =>
        {
            entity.ToTable("user_notifications", table =>
                table.HasCheckConstraint(
                    "CK_user_notifications_category",
                    "category IN ('Social', 'NewEpisode', 'Premiere')"));
            entity.HasKey(notification => notification.Id);
            entity.Property(notification => notification.Id).HasColumnName("id");
            entity.Property(notification => notification.AccountId).HasColumnName("account_id");
            entity.Property(notification => notification.Category)
                .HasColumnName("category")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(notification => notification.SourceKey)
                .HasColumnName("source_key")
                .HasMaxLength(160)
                .IsRequired();
            entity.Property(notification => notification.Title)
                .HasColumnName("title")
                .HasMaxLength(100)
                .IsRequired();
            entity.Property(notification => notification.Body)
                .HasColumnName("body")
                .HasMaxLength(240)
                .IsRequired();
            entity.Property(notification => notification.DeepLink)
                .HasColumnName("deep_link")
                .HasMaxLength(80)
                .IsRequired();
            entity.Property(notification => notification.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(notification => notification.ReadAt)
                .HasColumnName("read_at")
                .HasColumnType("timestamp with time zone");
            entity.HasIndex(notification => new
                {
                    notification.AccountId,
                    notification.SourceKey,
                })
                .IsUnique()
                .HasDatabaseName("UX_user_notifications_account_source");
            entity.HasIndex(notification => new
                {
                    notification.AccountId,
                    notification.CreatedAt,
                    notification.Id,
                })
                .HasDatabaseName("IX_user_notifications_account_created");
            entity.HasOne<Domain.Users.UserAccount>()
                .WithMany()
                .HasForeignKey(notification => notification.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NotificationDelivery>(entity =>
        {
            entity.ToTable("notification_deliveries", table =>
                table.HasCheckConstraint(
                    "CK_notification_deliveries_status",
                    "status IN ('Pending', 'Ticketed', 'Completed', 'Failed', 'Cancelled')"));
            entity.HasKey(delivery => delivery.Id);
            entity.Property(delivery => delivery.Id).HasColumnName("id");
            entity.Property(delivery => delivery.NotificationId).HasColumnName("notification_id");
            entity.Property(delivery => delivery.DeviceId).HasColumnName("device_id");
            entity.Property(delivery => delivery.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(12)
                .IsRequired();
            entity.Property(delivery => delivery.AttemptCount).HasColumnName("attempt_count");
            entity.Property(delivery => delivery.NextAttemptAt)
                .HasColumnName("next_attempt_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(delivery => delivery.LeaseId).HasColumnName("lease_id");
            entity.Property(delivery => delivery.LeaseUntil)
                .HasColumnName("lease_until")
                .HasColumnType("timestamp with time zone");
            entity.Property(delivery => delivery.ExpoTicketId)
                .HasColumnName("expo_ticket_id")
                .HasMaxLength(100);
            entity.Property(delivery => delivery.ExpoTicketCreatedAt)
                .HasColumnName("expo_ticket_created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(delivery => delivery.ReceiptCheckCount)
                .HasColumnName("receipt_check_count");
            entity.Property(delivery => delivery.ReceiptCheckAt)
                .HasColumnName("receipt_check_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(delivery => delivery.LastErrorCode)
                .HasColumnName("last_error_code")
                .HasMaxLength(80);
            entity.Property(delivery => delivery.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp with time zone");
            entity.HasIndex(delivery => new { delivery.NotificationId, delivery.DeviceId })
                .IsUnique()
                .HasDatabaseName("UX_notification_deliveries_notification_device");
            entity.HasIndex(delivery => new
                {
                    delivery.Status,
                    delivery.NextAttemptAt,
                    delivery.LeaseUntil,
                })
                .HasDatabaseName("IX_notification_deliveries_pending");
            entity.HasIndex(delivery => new { delivery.Status, delivery.ReceiptCheckAt })
                .HasDatabaseName("IX_notification_deliveries_receipts");
            entity.HasOne<UserNotification>()
                .WithMany()
                .HasForeignKey(delivery => delivery.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<NotificationDevice>()
                .WithMany()
                .HasForeignKey(delivery => delivery.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ProcessedNotificationEvent>(entity =>
        {
            entity.ToTable("notification_processed_social_events", table =>
                table.HasCheckConstraint(
                    "CK_notification_processed_social_events_outcome",
                    "outcome IN ('created', 'disabled', 'obsolete')"));
            entity.HasKey(processed => processed.EventId);
            entity.Property(processed => processed.EventId).HasColumnName("event_id");
            entity.Property(processed => processed.Outcome)
                .HasColumnName("outcome")
                .HasMaxLength(12)
                .IsRequired();
            entity.Property(processed => processed.ProcessedAt)
                .HasColumnName("processed_at")
                .HasColumnType("timestamp with time zone");
            entity.HasOne<Domain.Social.SocialEvent>()
                .WithOne()
                .HasForeignKey<ProcessedNotificationEvent>(processed => processed.EventId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
