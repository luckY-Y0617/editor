using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class PermissionNotificationConfiguration : IEntityTypeConfiguration<PermissionNotification>
{
    public void Configure(EntityTypeBuilder<PermissionNotification> builder)
    {
        builder.ToTable(
            "permission_notifications",
            table =>
            {
                table.HasCheckConstraint(
                    "permission_notifications_type_check",
                    "type IN ('access_request.created', 'access_request.approved', 'access_request.denied', 'permission.grant_created', 'permission.grant_updated', 'permission.grant_revoked', 'permission.grant_expiring', 'permission.grant_expired', 'group.member_added', 'group.member_removed', 'group.member_expiring', 'group.member_expired', 'share_link.created', 'share_link.revoked', 'email_invite.created', 'email_invite.accepted', 'email_invite.revoked', 'email_invite.delivery_failed')");
                table.HasCheckConstraint(
                    "permission_notifications_resource_type_check",
                    "resource_type IS NULL OR resource_type IN ('workspace', 'collection', 'document')");
            });

        builder.HasKey(notification => notification.Id);

        builder.Property(notification => notification.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(notification => notification.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(notification => notification.RecipientUserId).HasColumnName("recipient_user_id");
        builder.Property(notification => notification.ActorUserId).HasColumnName("actor_user_id");

        builder.Property(notification => notification.Type)
            .HasColumnName("type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(notification => notification.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text");

        builder.Property(notification => notification.ResourceId).HasColumnName("resource_id");
        builder.Property(notification => notification.AccessRequestId).HasColumnName("access_request_id");
        builder.Property(notification => notification.PermissionGrantId).HasColumnName("permission_grant_id");

        builder.Property(notification => notification.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(notification => notification.Body)
            .HasColumnName("body")
            .HasColumnType("text");

        builder.Property(notification => notification.ActionUrl)
            .HasColumnName("action_url")
            .HasColumnType("text");

        builder.Property(notification => notification.DedupeKey)
            .HasColumnName("dedupe_key")
            .HasColumnType("text");

        builder.Property(notification => notification.ReadAt).HasColumnName("read_at");

        builder.Property(notification => notification.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(notification => new { notification.RecipientUserId, notification.ReadAt, notification.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_permission_notifications_recipient_read_created");

        builder.HasIndex(notification => new { notification.WorkspaceId, notification.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_permission_notifications_workspace_created");

        builder.HasIndex(notification => notification.AccessRequestId)
            .HasDatabaseName("idx_permission_notifications_access_request");

        builder.HasIndex(notification => notification.DedupeKey)
            .IsUnique()
            .HasFilter("dedupe_key IS NOT NULL")
            .HasDatabaseName("permission_notifications_dedupe_key");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(notification => notification.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification => notification.RecipientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(notification => notification.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<AccessRequest>()
            .WithMany()
            .HasForeignKey(notification => notification.AccessRequestId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ResourceAccessGrant>()
            .WithMany()
            .HasForeignKey(notification => notification.PermissionGrantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
