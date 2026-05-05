using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ResourceEmailInviteConfiguration : IEntityTypeConfiguration<ResourceEmailInvite>
{
    public void Configure(EntityTypeBuilder<ResourceEmailInvite> builder)
    {
        builder.ToTable(
            "resource_email_invites",
            table =>
            {
                table.HasCheckConstraint("resource_email_invites_resource_type_check", "resource_type IN ('collection', 'document')");
                table.HasCheckConstraint("resource_email_invites_role_key_check", "role_key IN ('viewer', 'commenter')");
                table.HasCheckConstraint("resource_email_invites_status_check", "status IN ('pending', 'accepted', 'revoked', 'expired')");
                table.HasCheckConstraint("resource_email_invites_delivery_status_check", "delivery_status IN ('disabled', 'sent', 'failed')");
            });

        builder.HasKey(invite => invite.Id);

        builder.Property(invite => invite.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(invite => invite.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(invite => invite.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(invite => invite.ResourceId).HasColumnName("resource_id");

        builder.Property(invite => invite.Email)
            .HasColumnName("email")
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(invite => invite.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(invite => invite.RoleKey)
            .HasColumnName("role_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(invite => invite.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(invite => invite.InvitedBy).HasColumnName("invited_by");
        builder.Property(invite => invite.AcceptedBy).HasColumnName("accepted_by");
        builder.Property(invite => invite.RevokedBy).HasColumnName("revoked_by");

        builder.Property(invite => invite.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(invite => invite.ExpiresAt).HasColumnName("expires_at");
        builder.Property(invite => invite.AcceptedAt).HasColumnName("accepted_at");
        builder.Property(invite => invite.RevokedAt).HasColumnName("revoked_at");
        builder.Property(invite => invite.ExpiredAt).HasColumnName("expired_at");
        builder.Property(invite => invite.DeliveryStatus)
            .HasColumnName("delivery_status")
            .HasColumnType("text")
            .HasDefaultValue(EmailInviteDeliveryStatuses.Disabled)
            .IsRequired();

        builder.Property(invite => invite.DeliveryProvider)
            .HasColumnName("delivery_provider")
            .HasColumnType("text")
            .HasDefaultValue("noop")
            .IsRequired();

        builder.Property(invite => invite.DeliveryAttemptedAt).HasColumnName("delivery_attempted_at");

        builder.Property(invite => invite.DeliveryErrorCode)
            .HasColumnName("delivery_error_code")
            .HasColumnType("text");

        builder.HasIndex(invite => new { invite.WorkspaceId, invite.ResourceType, invite.ResourceId })
            .HasDatabaseName("idx_resource_email_invites_resource");

        builder.HasIndex(invite => invite.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_resource_email_invites_token_hash");

        builder.HasIndex(invite => new { invite.WorkspaceId, invite.ResourceType, invite.ResourceId, invite.Email })
            .IsUnique()
            .HasFilter("status = 'pending'")
            .HasDatabaseName("resource_email_invites_pending_resource_email_key");

        builder.HasIndex(invite => invite.ExpiresAt)
            .HasFilter("status = 'pending'")
            .HasDatabaseName("idx_resource_email_invites_pending_expiry");

        builder.HasIndex(invite => new { invite.DeliveryStatus, invite.CreatedAt })
            .HasDatabaseName("idx_resource_email_invites_delivery_status_created");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(invite => invite.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invite => invite.InvitedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invite => invite.AcceptedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(invite => invite.RevokedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
