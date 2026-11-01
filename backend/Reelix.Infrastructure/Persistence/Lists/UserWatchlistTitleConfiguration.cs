using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Lists;

public sealed class UserWatchlistTitleConfiguration : IEntityTypeConfiguration<UserWatchlistTitle>
{
    public void Configure(EntityTypeBuilder<UserWatchlistTitle> entity)
    {
        entity.ToTable("user_watchlist_titles");
        entity.HasKey(title => new { title.AccountId, title.TitleId });
        entity.Property(title => title.AccountId).HasColumnName("account_id");
        entity.Property(title => title.TitleId).HasColumnName("title_id");
        entity.Property(title => title.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(20)
            .IsRequired();
        entity.Property(title => title.AddedAt)
            .HasColumnName("added_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        entity.HasIndex(title => new { title.AccountId, title.AddedAt })
            .HasDatabaseName("IX_user_watchlist_titles_account_added_at");
        entity.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(title => title.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Domain.Catalog.ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(title => title.TitleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
