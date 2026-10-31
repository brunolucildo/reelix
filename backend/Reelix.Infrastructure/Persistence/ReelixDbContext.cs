using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Catalog;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence;

public sealed class ReelixDbContext(DbContextOptions<ReelixDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();

    public DbSet<UserGenrePreference> UserGenrePreferences => Set<UserGenrePreference>();

    public DbSet<UserFavoriteTitle> UserFavoriteTitles => Set<UserFavoriteTitle>();

    public DbSet<ExternalCatalogReference> ExternalCatalogReferences =>
        Set<ExternalCatalogReference>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
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
            entity.ToTable("user_profiles");
            entity.HasKey(profile => profile.AccountId);
            entity.Property(profile => profile.AccountId).HasColumnName("account_id");
            entity.Property(profile => profile.DisplayName)
                .HasColumnName("display_name")
                .HasMaxLength(50);
            entity.Property(profile => profile.Biography)
                .HasColumnName("biography")
                .HasMaxLength(280);
            entity.Property(profile => profile.ProfileIsPublic)
                .HasColumnName("profile_is_public")
                .HasDefaultValue(false);
            entity.Property(profile => profile.FavoritesArePublic)
                .HasColumnName("favorites_are_public")
                .HasDefaultValue(false);
            entity.Property(profile => profile.WatchHistoryIsPublic)
                .HasColumnName("watch_history_is_public")
                .HasDefaultValue(false);
            entity.HasOne<UserAccount>()
                .WithOne()
                .HasForeignKey<UserProfile>(profile => profile.AccountId)
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
