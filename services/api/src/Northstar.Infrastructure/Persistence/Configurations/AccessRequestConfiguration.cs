using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class AccessRequestConfiguration : IEntityTypeConfiguration<AccessRequest>
{
    public void Configure(EntityTypeBuilder<AccessRequest> builder)
    {
        builder.ToTable(
            "access_requests",
            table =>
            {
                table.HasCheckConstraint("access_requests_resource_type_check", "resource_type IN ('collection', 'document')");
                table.HasCheckConstraint("access_requests_subject_type_check", "subject_type IN ('user', 'group')");
                table.HasCheckConstraint("access_requests_requested_role_check", "requested_role IN ('owner', 'admin', 'editor', 'commenter', 'viewer')");
                table.HasCheckConstraint("access_requests_status_check", "status IN ('pending', 'approved', 'denied', 'cancelled')");
            });

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(request => request.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(request => request.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(request => request.ResourceId).HasColumnName("resource_id");
        builder.Property(request => request.RequesterId).HasColumnName("requester_id");

        builder.Property(request => request.SubjectType)
            .HasColumnName("subject_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(request => request.SubjectId).HasColumnName("subject_id");

        builder.Property(request => request.RequestedRole)
            .HasColumnName("requested_role")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(request => request.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(request => request.DecidedBy).HasColumnName("decided_by");
        builder.Property(request => request.DecidedAt).HasColumnName("decided_at");

        builder.Property(request => request.DecisionReason)
            .HasColumnName("decision_reason")
            .HasColumnType("text");

        builder.Property(request => request.ResultingGrantId).HasColumnName("resulting_grant_id");

        builder.Property(request => request.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(request => request.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(request => new { request.WorkspaceId, request.Status, request.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_access_requests_workspace_status_created");

        builder.HasIndex(request => new { request.ResourceType, request.ResourceId, request.Status })
            .HasDatabaseName("idx_access_requests_resource_status");

        builder.HasIndex(request => new { request.RequesterId, request.Status })
            .HasDatabaseName("idx_access_requests_requester_status");

        builder.HasIndex(request => new
            {
                request.WorkspaceId,
                request.ResourceType,
                request.ResourceId,
                request.SubjectType,
                request.SubjectId
            })
            .IsUnique()
            .HasFilter("status = 'pending'")
            .HasDatabaseName("access_requests_pending_subject_key");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(request => request.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.RequesterId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(request => request.DecidedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<ResourceAccessGrant>()
            .WithMany()
            .HasForeignKey(request => request.ResultingGrantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
