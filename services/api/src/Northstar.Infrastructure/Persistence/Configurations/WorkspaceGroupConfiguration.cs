using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceGroupConfiguration : IEntityTypeConfiguration<WorkspaceGroup>
{
    public void Configure(EntityTypeBuilder<WorkspaceGroup> builder)
    {
        builder.ToTable(
            "workspace_groups",
            table =>
            {
                table.HasCheckConstraint("workspace_groups_type_check", "type IN ('static', 'dynamic')");
            });

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(group => group.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(group => group.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(group => group.Description)
            .HasColumnName("description")
            .HasColumnType("text");

        builder.Property(group => group.Type)
            .HasColumnName("type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(group => group.ArchivedAt).HasColumnName("archived_at");

        builder.Property(group => group.ExternalProvider)
            .HasColumnName("external_provider")
            .HasColumnType("text");

        builder.Property(group => group.ExternalGroupId)
            .HasColumnName("external_group_id")
            .HasColumnType("text");

        builder.Property(group => group.ExternalSyncedAt)
            .HasColumnName("external_synced_at");

        builder.Property(group => group.CreatedBy).HasColumnName("created_by");

        builder.Property(group => group.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(group => group.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(group => new { group.WorkspaceId, group.Name })
            .IsUnique()
            .HasFilter("archived_at IS NULL")
            .HasDatabaseName("workspace_groups_workspace_name_active_key");

        builder.HasIndex(group => new { group.WorkspaceId, group.ArchivedAt })
            .HasDatabaseName("idx_workspace_groups_workspace_archived");

        builder.HasIndex(group => new { group.WorkspaceId, group.ExternalProvider, group.ExternalGroupId })
            .IsUnique()
            .HasFilter("external_provider IS NOT NULL AND external_group_id IS NOT NULL")
            .HasDatabaseName("workspace_groups_workspace_external_key");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(group => group.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(group => group.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
