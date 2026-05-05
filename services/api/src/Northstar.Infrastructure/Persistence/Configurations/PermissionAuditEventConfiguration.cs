using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class PermissionAuditEventConfiguration : IEntityTypeConfiguration<PermissionAuditEvent>
{
    public void Configure(EntityTypeBuilder<PermissionAuditEvent> builder)
    {
        builder.ToTable("permission_audit_events");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(audit => audit.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(audit => audit.ActorId).HasColumnName("actor_id");

        builder.Property(audit => audit.Action)
            .HasColumnName("action")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(audit => audit.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(audit => audit.ResourceId).HasColumnName("resource_id");

        builder.Property(audit => audit.SubjectType)
            .HasColumnName("subject_type")
            .HasColumnType("text");

        builder.Property(audit => audit.SubjectId).HasColumnName("subject_id");

        builder.Property(audit => audit.BeforeJson)
            .HasColumnName("before_json")
            .HasColumnType("jsonb");

        builder.Property(audit => audit.AfterJson)
            .HasColumnName("after_json")
            .HasColumnType("jsonb");

        builder.Property(audit => audit.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(audit => audit.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(audit => new { audit.WorkspaceId, audit.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_permission_audit_workspace_created");

        builder.HasIndex(audit => new { audit.ResourceType, audit.ResourceId, audit.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_permission_audit_resource_created");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(audit => audit.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(audit => audit.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
