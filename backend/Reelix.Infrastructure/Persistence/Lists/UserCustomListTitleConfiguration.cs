using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Reelix.Domain.Users;

namespace Reelix.Infrastructure.Persistence.Lists;

public sealed class UserCustomListTitleConfiguration : IEntityTypeConfiguration<UserCustomListTitle>
{
    public void Configure(EntityTypeBuilder<UserCustomListTitle> entity)
    {
        entity.ToTable("user_custom_list_titles");
        entity.HasKey(title => new { title.ListId, title.TitleId });
        entity.Property(title => title.ListId).HasColumnName("list_id");
        entity.Property(title => title.TitleId).HasColumnName("title_id");
        entity.Property(title => title.ContentType)
            .HasColumnName("content_type")
            .HasMaxLength(20)
            .IsRequired();
        entity.Property(title => title.Position).HasColumnName("position").IsRequired();
        entity.Property(title => title.AddedAt)
            .HasColumnName("added_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();
        entity.HasIndex(title => new { title.ListId, title.Position })
            .HasDatabaseName("IX_user_custom_list_titles_list_position");
        entity.HasOne<UserCustomList>()
            .WithMany()
            .HasForeignKey(title => title.ListId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<Domain.Catalog.ExternalCatalogReference>()
            .WithMany()
            .HasForeignKey(title => title.TitleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
