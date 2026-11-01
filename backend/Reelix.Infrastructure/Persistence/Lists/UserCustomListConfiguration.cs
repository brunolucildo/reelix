using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Lists;

public sealed class UserCustomListConfiguration : IEntityTypeConfiguration<UserCustomList>
{
    public void Configure(EntityTypeBuilder<UserCustomList> entity)
    {
        entity.ToTable("user_custom_lists");
        entity.HasKey(list => list.Id);
        entity.Property(list => list.Id).HasColumnName("id");
        entity.Property(list => list.AccountId).HasColumnName("account_id").IsRequired();
        entity.Property(list => list.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
        entity.Property(list => list.Description).HasColumnName("description").HasMaxLength(500);
        entity.Property(list => list.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        entity.Property(list => list.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        entity.HasIndex(list => new { list.AccountId, list.UpdatedAt })
            .HasDatabaseName("IX_user_custom_lists_account_updated_at");
        entity.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(list => list.AccountId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
