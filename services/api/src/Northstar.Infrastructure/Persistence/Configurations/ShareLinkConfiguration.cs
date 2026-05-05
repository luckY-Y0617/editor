using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ShareLinkConfiguration : IEntityTypeConfiguration<ShareLink>
{
    public void Configure(EntityTypeBuilder<ShareLink> builder)
    {
        builder.ToTable(
            "share_links",
            table =>
            {
                table.HasCheckConstraint("share_links_resource_type_check", "resource_type IN ('collection', 'document')");
                table.HasCheckConstraint("share_links_role_key_check", "role_key IN ('viewer', 'commenter')");
                table.HasCheckConstraint("share_links_audience_check", "audience IN ('workspace', 'external', 'public')");
                table.HasCheckConstraint("share_links_public_viewer_expiry_check", "audience <> 'public' OR (resource_type IN ('document', 'collection') AND role_key = 'viewer' AND subject_email IS NULL AND expires_at IS NOT NULL)");
                table.HasCheckConstraint("share_links_password_public_check", "password_hash IS NULL OR audience = 'public'");
            });

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(link => link.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(link => link.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(link => link.ResourceId).HasColumnName("resource_id");

        builder.Property(link => link.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(link => link.RoleKey)
            .HasColumnName("role_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(link => link.Audience)
            .HasColumnName("audience")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(link => link.SubjectEmail)
            .HasColumnName("subject_email")
            .HasColumnType("citext");

        builder.Property(link => link.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text");

        builder.Property(link => link.CreatedBy).HasColumnName("created_by");

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(link => link.ExpiresAt).HasColumnName("expires_at");
        builder.Property(link => link.RevokedAt).HasColumnName("revoked_at");

        builder.HasIndex(link => new { link.WorkspaceId, link.ResourceType, link.ResourceId })
            .HasDatabaseName("idx_share_links_resource");

        builder.HasIndex(link => link.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_share_links_token_hash");

        builder.HasIndex(link => link.ExpiresAt)
            .HasFilter("revoked_at IS NULL")
            .HasDatabaseName("idx_share_links_expiry");

        builder.HasIndex(link => new { link.WorkspaceId, link.ResourceType, link.ResourceId, link.ExpiresAt })
            .HasFilter("audience = 'public' AND revoked_at IS NULL")
            .HasDatabaseName("idx_share_links_public_active");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(link => link.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
