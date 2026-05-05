using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Tags;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(tag => tag.Id);

        builder.Property(tag => tag.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(tag => tag.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(tag => tag.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(tag => tag.Slug)
            .HasColumnName("slug")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(tag => tag.Color).HasColumnName("color").HasColumnType("text");
        builder.Property(tag => tag.CreatedBy).HasColumnName("created_by");

        builder.Property(tag => tag.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(tag => tag.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(tag => tag.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(tag => new { tag.WorkspaceId, tag.Slug })
            .IsUnique()
            .HasDatabaseName("tags_workspace_slug_key");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(tag => tag.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(tag => tag.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

