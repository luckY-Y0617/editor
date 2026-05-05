using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class WorkspaceGroupMemberConfiguration : IEntityTypeConfiguration<WorkspaceGroupMember>
{
    public void Configure(EntityTypeBuilder<WorkspaceGroupMember> builder)
    {
        builder.ToTable("workspace_group_members");

        builder.HasKey(member => member.Id);

        builder.Property(member => member.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(member => member.GroupId).HasColumnName("group_id");
        builder.Property(member => member.UserId).HasColumnName("user_id");
        builder.Property(member => member.AddedBy).HasColumnName("added_by");

        builder.Property(member => member.AddedAt)
            .HasColumnName("added_at")
            .HasDefaultValueSql("now()");

        builder.Property(member => member.ExpiresAt).HasColumnName("expires_at");
        builder.Property(member => member.RemovedAt).HasColumnName("removed_at");

        builder.HasIndex(member => new { member.GroupId, member.UserId })
            .IsUnique()
            .HasFilter("removed_at IS NULL")
            .HasDatabaseName("workspace_group_members_group_user_active_key");

        builder.HasIndex(member => new { member.UserId, member.RemovedAt, member.ExpiresAt })
            .HasDatabaseName("idx_workspace_group_members_user_active");

        builder.HasOne<WorkspaceGroup>()
            .WithMany()
            .HasForeignKey(member => member.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(member => member.AddedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
