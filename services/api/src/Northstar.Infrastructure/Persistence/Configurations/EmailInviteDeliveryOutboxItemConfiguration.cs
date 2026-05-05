using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class EmailInviteDeliveryOutboxItemConfiguration : IEntityTypeConfiguration<EmailInviteDeliveryOutboxItem>
{
    public void Configure(EntityTypeBuilder<EmailInviteDeliveryOutboxItem> builder)
    {
        builder.ToTable(
            "email_invite_delivery_outbox",
            table =>
            {
                table.HasCheckConstraint(
                    "email_invite_delivery_outbox_status_check",
                    "status IN ('pending', 'retry_scheduled', 'sent', 'failed')");
                table.HasCheckConstraint(
                    "email_invite_delivery_outbox_attempts_check",
                    "attempt_count >= 0 AND max_attempts > 0 AND attempt_count <= max_attempts");
            });

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(item => item.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(item => item.InviteId).HasColumnName("invite_id");

        builder.Property(item => item.RecipientEmail)
            .HasColumnName("recipient_email")
            .HasColumnType("citext")
            .IsRequired();

        builder.Property(item => item.Provider)
            .HasColumnName("provider")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(item => item.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(EmailInviteDeliveryOutboxStatuses.Pending)
            .IsRequired();

        builder.Property(item => item.AttemptCount)
            .HasColumnName("attempt_count")
            .HasDefaultValue(0);

        builder.Property(item => item.MaxAttempts)
            .HasColumnName("max_attempts")
            .HasDefaultValue(3);

        builder.Property(item => item.NextAttemptAt).HasColumnName("next_attempt_at");
        builder.Property(item => item.LastAttemptAt).HasColumnName("last_attempt_at");
        builder.Property(item => item.SentAt).HasColumnName("sent_at");
        builder.Property(item => item.FailedAt).HasColumnName("failed_at");

        builder.Property(item => item.LastErrorCode)
            .HasColumnName("last_error_code")
            .HasColumnType("text");

        builder.Property(item => item.LastErrorMessage)
            .HasColumnName("last_error_message")
            .HasColumnType("text");

        builder.Property(item => item.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(item => item.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(item => new { item.Status, item.NextAttemptAt, item.CreatedAt })
            .HasDatabaseName("idx_email_invite_delivery_outbox_due");

        builder.HasIndex(item => new { item.WorkspaceId, item.InviteId })
            .HasDatabaseName("idx_email_invite_delivery_outbox_invite");

        builder.HasIndex(item => new { item.Status, item.FailedAt })
            .HasDatabaseName("idx_email_invite_delivery_outbox_failed");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(item => item.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ResourceEmailInvite>()
            .WithMany()
            .HasForeignKey(item => item.InviteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
