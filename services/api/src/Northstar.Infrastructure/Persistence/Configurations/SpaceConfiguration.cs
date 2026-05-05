using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class SpaceConfiguration : IEntityTypeConfiguration<Space>
{
    public void Configure(EntityTypeBuilder<Space> builder)
    {
        builder.ToTable(
            "spaces",
            table => table.HasCheckConstraint(
                "spaces_visibility_check",
                "visibility IN ('private', 'workspace', 'public')"));

        builder.HasKey(space => space.Id);

        builder.Property(space => space.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(space => space.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(space => space.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(space => space.Slug)
            .HasColumnName("slug")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(space => space.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(space => space.Visibility)
            .HasColumnName("visibility")
            .HasColumnType("text")
            .HasDefaultValue(SpaceVisibility.Workspace)
            .IsRequired();

        builder.Property(space => space.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("numeric(20,10)")
            .HasDefaultValue(0m);

        builder.Property(space => space.CreatedBy).HasColumnName("created_by");

        builder.Property(space => space.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(space => space.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(space => space.ArchivedAt).HasColumnName("archived_at");
        builder.Property(space => space.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(space => new { space.WorkspaceId, space.Slug })
            .IsUnique()
            .HasDatabaseName("spaces_workspace_slug_key");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(space => space.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(space => space.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
