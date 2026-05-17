using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ResourceAccessPolicyConfiguration : IEntityTypeConfiguration<ResourceAccessPolicy>
{
    public void Configure(EntityTypeBuilder<ResourceAccessPolicy> builder)
    {
        builder.ToTable(
            "resource_access_policies",
            table =>
            {
                table.HasCheckConstraint("resource_access_policies_resource_type_check", "resource_type IN ('library', 'collection', 'document')");
                table.HasCheckConstraint("resource_access_policies_inheritance_mode_check", "inheritance_mode IN ('inherit', 'restricted')");
                table.HasCheckConstraint("resource_access_policies_link_mode_check", "link_mode IN ('disabled', 'internal', 'external', 'public')");
                table.HasCheckConstraint("resource_access_policies_default_link_role_check", "default_link_role IS NULL OR default_link_role IN ('viewer', 'commenter')");
            });

        builder.HasKey(policy => policy.Id);

        builder.Property(policy => policy.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(policy => policy.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(policy => policy.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(policy => policy.ResourceId).HasColumnName("resource_id");

        builder.Property(policy => policy.InheritanceMode)
            .HasColumnName("inheritance_mode")
            .HasColumnType("text")
            .HasDefaultValue(InheritanceModes.Inherit)
            .IsRequired();

        builder.Property(policy => policy.LinkMode)
            .HasColumnName("link_mode")
            .HasColumnType("text")
            .HasDefaultValue(LinkModes.Disabled)
            .IsRequired();

        builder.Property(policy => policy.DefaultLinkRole)
            .HasColumnName("default_link_role")
            .HasColumnType("text");

        builder.Property(policy => policy.CreatedBy).HasColumnName("created_by");

        builder.Property(policy => policy.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(policy => policy.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(policy => new { policy.ResourceType, policy.ResourceId })
            .IsUnique()
            .HasDatabaseName("idx_policies_resource");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(policy => policy.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(policy => policy.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
