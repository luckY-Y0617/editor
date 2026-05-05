using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceMemberConfiguration : IEntityTypeConfiguration<WorkspaceMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceMember> builder)
    {
        builder.ToTable(
            "workspace_members",
            table =>
            {
                table.HasCheckConstraint("workspace_members_role_check", "role IN ('owner', 'admin', 'editor', 'viewer')");
                table.HasCheckConstraint("workspace_members_status_check", "status IN ('invited', 'active', 'suspended')");
            });

        builder.HasKey(member => new { member.WorkspaceId, member.UserId });

        builder.Property(member => member.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(member => member.UserId).HasColumnName("user_id");

        builder.Property(member => member.Role)
            .HasColumnName("role")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(member => member.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(WorkspaceMemberStatus.Active)
            .IsRequired();

        builder.Property(member => member.JoinedAt).HasColumnName("joined_at");

        builder.Property(member => member.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(member => member.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

