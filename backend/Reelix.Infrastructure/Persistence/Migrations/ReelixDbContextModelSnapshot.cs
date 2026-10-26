using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Reelix.Domain.Catalog;
using Reelix.Domain.Users;
using Reelix.Infrastructure.Persistence;

#nullable disable

namespace Reelix.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ReelixDbContext))]
public sealed class ReelixDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.13")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.Property(account => account.Id)
                .HasColumnType("uuid")
                .HasColumnName("id");
            entity.Property(account => account.SupabaseUserId)
                .HasColumnType("uuid")
                .HasColumnName("supabase_user_id");
            entity.Property(account => account.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.HasKey(account => account.Id);
            entity.HasIndex(account => account.SupabaseUserId)
                .HasDatabaseName("IX_user_accounts_supabase_user_id")
                .IsUnique();
            entity.ToTable("user_accounts");
        });

        modelBuilder.Entity<ExternalCatalogReference>(entity =>
        {
            entity.Property(reference => reference.Id)
                .HasColumnType("uuid")
                .HasColumnName("id");
            entity.Property(reference => reference.Provider)
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("provider");
            entity.Property(reference => reference.ExternalId)
                .HasMaxLength(64)
                .HasColumnType("character varying(64)")
                .HasColumnName("external_id");
            entity.Property(reference => reference.ContentType)
                .HasMaxLength(20)
                .HasColumnType("character varying(20)")
                .HasColumnName("content_type");
            entity.Property(reference => reference.CreatedAt)
                .HasColumnType("timestamp with time zone")
                .HasColumnName("created_at");
            entity.HasKey(reference => reference.Id);
            entity.HasIndex(reference => new
            {
                reference.Provider,
                reference.ExternalId,
                reference.ContentType,
            })
                .HasDatabaseName("UX_catalog_external_references_external_key")
                .IsUnique();
            entity.ToTable("catalog_external_references");
        });
    }
}
