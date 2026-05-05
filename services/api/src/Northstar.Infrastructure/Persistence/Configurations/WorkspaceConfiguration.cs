using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceConfiguration : IEntityTypeConfiguration<Workspace>
{
    public void Configure(EntityTypeBuilder<Workspace> builder)
    {
        builder.ToTable("workspaces");

        builder.HasKey(workspace => workspace.Id);

        builder.Property(workspace => workspace.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(workspace => workspace.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(workspace => workspace.Slug)
            .HasColumnName("slug")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(workspace => workspace.CreatedBy).HasColumnName("created_by");
        builder.Property(workspace => workspace.DefaultSpaceId).HasColumnName("default_space_id");

        builder.Property(workspace => workspace.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(workspace => workspace.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(workspace => workspace.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(workspace => workspace.Slug)
            .IsUnique()
            .HasDatabaseName("workspaces_slug_key");

        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(workspace => workspace.DefaultSpaceId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(workspace => workspace.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
