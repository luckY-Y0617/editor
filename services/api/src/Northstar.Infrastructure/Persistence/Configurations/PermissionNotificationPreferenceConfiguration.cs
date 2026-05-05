using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class PermissionNotificationPreferenceConfiguration : IEntityTypeConfiguration<PermissionNotificationPreference>
{
    public void Configure(EntityTypeBuilder<PermissionNotificationPreference> builder)
    {
        builder.ToTable(
            "permission_notification_preferences",
            table =>
            {
                table.HasCheckConstraint(
                    "permission_notification_preferences_scope_check",
                    "(resource_type IS NULL AND resource_id IS NULL) OR (resource_type IS NOT NULL AND resource_id IS NOT NULL)");
                table.HasCheckConstraint(
                    "permission_notification_preferences_resource_type_check",
                    "resource_type IS NULL OR resource_type IN ('collection', 'document')");
                table.HasCheckConstraint(
                    "permission_notification_preferences_watch_mute_check",
                    "NOT (watched AND muted)");
            });

        builder.HasKey(preference => preference.Id);

        builder.Property(preference => preference.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(preference => preference.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(preference => preference.UserId).HasColumnName("user_id");

        builder.Property(preference => preference.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text");

        builder.Property(preference => preference.ResourceId).HasColumnName("resource_id");
        builder.Property(preference => preference.Watched).HasColumnName("watched");
        builder.Property(preference => preference.Muted).HasColumnName("muted");

        builder.Property(preference => preference.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(preference => preference.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(preference => new { preference.WorkspaceId, preference.UserId })
            .IsUnique()
            .HasFilter("resource_type IS NULL AND resource_id IS NULL")
            .HasDatabaseName("permission_notification_preferences_workspace_user_key");

        builder.HasIndex(preference => new { preference.WorkspaceId, preference.UserId, preference.ResourceType, preference.ResourceId })
            .IsUnique()
            .HasFilter("resource_type IS NOT NULL AND resource_id IS NOT NULL")
            .HasDatabaseName("permission_notification_preferences_resource_user_key");

        builder.HasIndex(preference => new { preference.UserId, preference.WorkspaceId })
            .HasDatabaseName("idx_permission_notification_preferences_user_workspace");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(preference => preference.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(preference => preference.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
