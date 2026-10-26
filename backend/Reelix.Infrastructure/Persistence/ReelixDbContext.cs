using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Catalog;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence;

public sealed class ReelixDbContext(DbContextOptions<ReelixDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

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
