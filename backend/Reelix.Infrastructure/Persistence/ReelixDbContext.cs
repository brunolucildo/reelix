using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Catalog;
using Reelix.Domain.Ratings;
using Reelix.Domain.Notifications;
using Reelix.Domain.Gamification;
using Reelix.Domain.Social;
using Reelix.Domain.Tracking;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence.Catalog;
using Reelix.Infrastructure.Persistence.Lists;
using Reelix.Infrastructure.Persistence.Ratings;
using Reelix.Infrastructure.Persistence.Notifications;
using Reelix.Infrastructure.Persistence.Gamification;
using Reelix.Infrastructure.Persistence.Tracking;

namespace Reelix.Infrastructure.Persistence;

public sealed class ReelixDbContext(DbContextOptions<ReelixDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    public DbSet<UserGenrePreference> UserGenrePreferences => Set<UserGenrePreference>();

    public DbSet<UserFavoriteTitle> UserFavoriteTitles => Set<UserFavoriteTitle>();

    public DbSet<TrackingRecord> TrackingRecords => Set<TrackingRecord>();

    public DbSet<UserWatchlistTitle> UserWatchlistTitles => Set<UserWatchlistTitle>();

    public DbSet<UserCustomList> UserCustomLists => Set<UserCustomList>();

    public DbSet<UserCustomListTitle> UserCustomListTitles => Set<UserCustomListTitle>();

    public DbSet<ExternalCatalogReference> ExternalCatalogReferences =>
        Set<ExternalCatalogReference>();

    public DbSet<CatalogReferenceGenre> CatalogReferenceGenres => Set<CatalogReferenceGenre>();

    public DbSet<CatalogEpisodeSeries> CatalogEpisodeSeries => Set<CatalogEpisodeSeries>();

    public DbSet<UserRating> UserRatings => Set<UserRating>();

    public DbSet<UserComment> UserComments => Set<UserComment>();

    public DbSet<FriendRelationship> FriendRelationships => Set<FriendRelationship>();

    public DbSet<SocialEvent> SocialEvents => Set<SocialEvent>();

    public DbSet<NotificationPreferences> NotificationPreferences => Set<NotificationPreferences>();

    public DbSet<NotificationDevice> NotificationDevices => Set<NotificationDevice>();

    public DbSet<UserNotification> UserNotifications => Set<UserNotification>();

    public DbSet<NotificationDelivery> NotificationDeliveries => Set<NotificationDelivery>();

    public DbSet<ProcessedNotificationEvent> ProcessedNotificationEvents =>
        Set<ProcessedNotificationEvent>();

    public DbSet<AchievementDefinition> AchievementDefinitions => Set<AchievementDefinition>();

    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();

    public DbSet<GamificationPreferences> GamificationPreferences => Set<GamificationPreferences>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new TrackingRecordConfiguration());
        modelBuilder.ApplyConfiguration(new UserRatingConfiguration());
        modelBuilder.ApplyConfiguration(new UserCommentConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogEpisodeSeriesConfiguration());
        modelBuilder.ApplyConfiguration(new CatalogReferenceGenreConfiguration());
        ListsPersistenceConfiguration.Apply(modelBuilder);
        NotificationsPersistenceConfiguration.Apply(modelBuilder);
        GamificationPersistenceConfiguration.Apply(modelBuilder);

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("user_accounts");
            entity.HasKey(account => account.Id);
            entity.Property(account => account.Id).HasColumnName("id");
            entity.Property(account => account.SupabaseUserId)
                .HasColumnName("supabase_user_id")
                .IsRequired();
            entity.Property(account => account.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasIndex(account => account.SupabaseUserId)
                .HasDatabaseName("IX_user_accounts_supabase_user_id")
                .IsUnique();
        });

        modelBuilder.Entity<UserProfile>(entity =>
        {
            entity.ToTable("user_profiles", table =>
            {
                table.HasCheckConstraint(
                    "CK_user_profiles_friend_history_consent",
                    "watch_history_shared_with_friends = (watch_history_shared_with_friends_since IS NOT NULL)");
                table.HasCheckConstraint(
                    "CK_user_profiles_friend_ratings_consent",
                    "ratings_shared_with_friends = (ratings_shared_with_friends_since IS NOT NULL)");
                table.HasCheckConstraint("CK_user_profiles_avatar_key", "avatar_key IS NULL OR avatar_key IN ('noir', 'cosmic', 'phantom', 'comedy', 'animation', 'adventure', 'robot', 'vampire', 'maestro')");
            });
            entity.HasKey(profile => profile.AccountId);
            entity.Property(profile => profile.AccountId).HasColumnName("account_id");
            entity.Property(profile => profile.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(50);
            entity.Property(profile => profile.Biography)
                .HasColumnName("biography")
                .HasMaxLength(280);
            entity.Property(profile => profile.AvatarKey).HasColumnName("avatar_key").HasMaxLength(24);
            entity.Property(profile => profile.ProfileIsPublic)
                .HasColumnName("profile_is_public")
                .HasDefaultValue(false);
            entity.Property(profile => profile.FavoritesArePublic)
                .HasColumnName("favorites_are_public")
                .HasDefaultValue(false);
            entity.Property(profile => profile.WatchHistoryIsPublic)
                .HasColumnName("watch_history_is_public")
                .HasDefaultValue(false);
            entity.Property(profile => profile.IsDiscoverable)
                .HasColumnName("is_discoverable")
                .HasDefaultValue(false);
            entity.Property(profile => profile.WatchHistorySharedWithFriends)
                .HasColumnName("watch_history_shared_with_friends")
                .HasDefaultValue(false);
            entity.Property(profile => profile.WatchHistorySharedWithFriendsSince)
                .HasColumnName("watch_history_shared_with_friends_since")
                .HasColumnType("timestamp with time zone");
            entity.Property(profile => profile.FavoritesSharedWithFriends)
                .HasColumnName("favorites_shared_with_friends")
                .HasDefaultValue(false);
            entity.Property(profile => profile.RatingsSharedWithFriends)
                .HasColumnName("ratings_shared_with_friends")
                .HasDefaultValue(false);
            entity.Property(profile => profile.RatingsSharedWithFriendsSince)
                .HasColumnName("ratings_shared_with_friends_since")
                .HasColumnType("timestamp with time zone");
            entity.HasIndex(profile => profile.DisplayName)
                .HasDatabaseName("IX_user_profiles_display_name");
            entity.HasOne<UserAccount>()
                .WithOne()
                .HasForeignKey<UserProfile>(profile => profile.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<FriendRelationship>(entity =>
        {
            entity.ToTable("friend_relationships", table =>
            {
                table.HasCheckConstraint(
                    "CK_friend_relationships_distinct_accounts",
                    "first_account_id <> second_account_id");
                table.HasCheckConstraint(
                    "CK_friend_relationships_canonical_order",
                    "first_account_id < second_account_id");
                table.HasCheckConstraint(
                    "CK_friend_relationships_participants",
                    "(requester_account_id = first_account_id AND addressee_account_id = second_account_id) OR " +
                    "(requester_account_id = second_account_id AND addressee_account_id = first_account_id)");
                table.HasCheckConstraint(
                    "CK_friend_relationships_status",
                    "status IN ('Pending', 'Accepted', 'Declined', 'Cancelled', 'Removed')");
            });
            entity.HasKey(relationship => relationship.Id);
            entity.Property(relationship => relationship.Id).HasColumnName("id");
            entity.Property(relationship => relationship.FirstAccountId).HasColumnName("first_account_id");
            entity.Property(relationship => relationship.SecondAccountId).HasColumnName("second_account_id");
            entity.Property(relationship => relationship.RequesterAccountId).HasColumnName("requester_account_id");
            entity.Property(relationship => relationship.AddresseeAccountId).HasColumnName("addressee_account_id");
            entity.Property(relationship => relationship.Status)
                .HasColumnName("status")
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(relationship => relationship.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(relationship => relationship.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .IsConcurrencyToken();
            entity.HasIndex(relationship => new
                {
                    relationship.FirstAccountId,
                    relationship.SecondAccountId,
                })
                .IsUnique()
                .HasDatabaseName("UX_friend_relationships_pair");
            entity.HasIndex(relationship => new
                {
                    relationship.AddresseeAccountId,
                    relationship.Status,
                    relationship.UpdatedAt,
                })
                .HasDatabaseName("IX_friend_relationships_incoming");
            entity.HasIndex(relationship => new
                {
                    relationship.RequesterAccountId,
                    relationship.Status,
                    relationship.UpdatedAt,
                })
                .HasDatabaseName("IX_friend_relationships_outgoing");
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(relationship => relationship.FirstAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(relationship => relationship.SecondAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SocialEvent>(entity =>
        {
            entity.ToTable("social_events", table =>
            {
                table.HasCheckConstraint(
                    "CK_social_events_distinct_accounts",
                    "actor_account_id <> recipient_account_id");
                table.HasCheckConstraint(
                    "CK_social_events_version_and_type",
                    "event_version > 0 AND event_type IN ('friend.requested', 'friend.accepted')");
            });
            entity.HasKey(socialEvent => socialEvent.Id);
            entity.Property(socialEvent => socialEvent.Id).HasColumnName("id");
            entity.Property(socialEvent => socialEvent.EventType)
                .HasColumnName("event_type")
                .HasMaxLength(40)
                .IsRequired();
            entity.Property(socialEvent => socialEvent.EventVersion)
                .HasColumnName("event_version")
                .IsRequired();
            entity.Property(socialEvent => socialEvent.ActorAccountId).HasColumnName("actor_account_id");
            entity.Property(socialEvent => socialEvent.RecipientAccountId).HasColumnName("recipient_account_id");
            entity.Property(socialEvent => socialEvent.OccurredAt)
                .HasColumnName("occurred_at")
                .HasColumnType("timestamp with time zone");
            entity.HasIndex(socialEvent => new
                {
                    socialEvent.RecipientAccountId,
                    socialEvent.OccurredAt,
                })
                .HasDatabaseName("IX_social_events_recipient_occurred_at");
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(socialEvent => socialEvent.ActorAccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<UserAccount>()
                .WithMany()
                .HasForeignKey(socialEvent => socialEvent.RecipientAccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserPreferences>(entity =>
        {
            entity.ToTable("user_preferences");
            entity.HasKey(preferences => preferences.AccountId);
            entity.Property(preferences => preferences.AccountId).HasColumnName("account_id");
            entity.Property(preferences => preferences.CurrentStep)
                .HasColumnName("current_step")
                .HasMaxLength(30)
                .IsRequired();
            entity.Property(preferences => preferences.CompletedAt)
                .HasColumnName("completed_at")
                .HasColumnType("timestamp with time zone");
            entity.HasOne<UserAccount>()
                .WithOne()
                .HasForeignKey<UserPreferences>(preferences => preferences.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserGenrePreference>(entity =>
        {
            entity.ToTable("user_genre_preferences");
            entity.HasKey(preference => new
            {
                preference.AccountId,
                preference.ContentType,
                preference.GenreId,
            });
            entity.Property(preference => preference.AccountId).HasColumnName("account_id");
            entity.Property(preference => preference.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(preference => preference.GenreId).HasColumnName("genre_id");
            entity.HasOne<UserPreferences>()
                .WithMany(preferences => preferences.GenrePreferences)
                .HasForeignKey(preference => preference.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserFavoriteTitle>(entity =>
        {
            entity.ToTable("user_favorite_titles");
            entity.HasKey(favorite => new { favorite.AccountId, favorite.TitleId });
            entity.Property(favorite => favorite.AccountId).HasColumnName("account_id");
            entity.Property(favorite => favorite.TitleId).HasColumnName("title_id");
            entity.Property(favorite => favorite.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(20)
                .IsRequired();
            entity.HasOne<UserPreferences>()
                .WithMany(preferences => preferences.FavoriteTitles)
                .HasForeignKey(favorite => favorite.AccountId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ExternalCatalogReference>()
                .WithMany()
                .HasForeignKey(favorite => favorite.TitleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ExternalCatalogReference>(entity =>
        {
            entity.ToTable("catalog_external_references");
            entity.HasKey(reference => reference.Id);
            entity.Property(reference => reference.Id).HasColumnName("id");
            entity.Property(reference => reference.Provider)
                .HasColumnName("provider")
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(reference => reference.ExternalId)
                .HasColumnName("external_id")
                .HasMaxLength(64)
                .IsRequired();
            entity.Property(reference => reference.ContentType)
                .HasColumnName("content_type")
                .HasMaxLength(20)
                .IsRequired();
            entity.Property(reference => reference.RuntimeMinutes)
                .HasColumnName("runtime_minutes");
            entity.Property(reference => reference.MetadataUpdatedAt)
                .HasColumnName("metadata_updated_at")
                .HasColumnType("timestamp with time zone");
            entity.Property(reference => reference.MetadataVersion)
                .HasColumnName("metadata_version")
                .HasDefaultValue(0)
                .IsRequired();
            entity.Property(reference => reference.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .IsRequired();
            entity.HasIndex(reference => new
            {
                reference.Provider,
                reference.ExternalId,
                reference.ContentType,
            })
                .HasDatabaseName("UX_catalog_external_references_external_key")
                .IsUnique();
        });
    }
}
