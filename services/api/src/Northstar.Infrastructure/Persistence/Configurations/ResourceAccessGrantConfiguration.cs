using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ResourceAccessGrantConfiguration : IEntityTypeConfiguration<ResourceAccessGrant>
{
    public void Configure(EntityTypeBuilder<ResourceAccessGrant> builder)
    {
        builder.ToTable(
            "resource_access_grants",
            table =>
            {
                table.HasCheckConstraint("resource_access_grants_resource_type_check", "resource_type IN ('collection', 'document')");
                table.HasCheckConstraint("resource_access_grants_subject_type_check", "subject_type IN ('user', 'group')");
                table.HasCheckConstraint("resource_access_grants_role_key_check", "role_key IN ('owner', 'admin', 'editor', 'commenter', 'viewer')");
            });

        builder.HasKey(grant => grant.Id);

        builder.Property(grant => grant.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(grant => grant.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(grant => grant.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(grant => grant.ResourceId).HasColumnName("resource_id");

        builder.Property(grant => grant.SubjectType)
            .HasColumnName("subject_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(grant => grant.SubjectId).HasColumnName("subject_id");

        builder.Property(grant => grant.RoleKey)
            .HasColumnName("role_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(grant => grant.GrantedBy).HasColumnName("granted_by");

        builder.Property(grant => grant.GrantedAt)
            .HasColumnName("granted_at")
            .HasDefaultValueSql("now()");

        builder.Property(grant => grant.ExpiresAt).HasColumnName("expires_at");
        builder.Property(grant => grant.RevokedAt).HasColumnName("revoked_at");
        builder.Property(grant => grant.RevokedBy).HasColumnName("revoked_by");

        builder.Property(grant => grant.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.HasIndex(grant => new
            {
                grant.ResourceType,
                grant.ResourceId,
                grant.SubjectType,
                grant.SubjectId
            })
            .IsUnique()
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("resource_access_grants_resource_subject_key");

        builder.HasIndex(grant => new { grant.WorkspaceId, grant.ResourceType, grant.ResourceId })
            .HasDatabaseName("idx_grants_workspace_resource");

        builder.HasIndex(grant => new { grant.WorkspaceId, grant.SubjectType, grant.SubjectId })
            .HasDatabaseName("idx_grants_subject");

        builder.HasIndex(grant => new { grant.WorkspaceId, grant.ResourceType, grant.ResourceId, grant.SubjectType })
            .HasDatabaseName("idx_grants_workspace_resource_subject_type");

        builder.HasIndex(grant => grant.ExpiresAt)
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("idx_grants_expiry");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(grant => grant.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(grant => grant.GrantedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(grant => grant.RevokedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
