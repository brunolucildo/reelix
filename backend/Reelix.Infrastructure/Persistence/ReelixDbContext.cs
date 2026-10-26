using Microsoft.EntityFrameworkCore;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence;

public sealed class ReelixDbContext(DbContextOptions<ReelixDbContext> options)
    : DbContext(options)
{
    public DbSet<UserAccount> UserAccounts => Set<UserAccount>();

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
    }
}
