using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ShareLinkAccessStatsConfiguration : IEntityTypeConfiguration<ShareLinkAccessStats>
{
    public void Configure(EntityTypeBuilder<ShareLinkAccessStats> builder)
    {
        builder.ToTable("share_link_access_stats");

        builder.HasKey(stats => stats.ShareLinkId);

        builder.Property(stats => stats.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(stats => stats.ShareLinkId).HasColumnName("share_link_id");
        builder.Property(stats => stats.LastAccessedAt).HasColumnName("last_accessed_at");
        builder.Property(stats => stats.AccessCount)
            .HasColumnName("access_count")
            .HasDefaultValue(0L);
        builder.Property(stats => stats.UniqueVisitorCount)
            .HasColumnName("unique_visitor_count")
            .HasDefaultValue(0L);
        builder.Property(stats => stats.LastAccessIp)
            .HasColumnName("last_access_ip")
            .HasColumnType("text");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(stats => stats.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShareLink>()
            .WithOne()
            .HasForeignKey<ShareLinkAccessStats>(stats => stats.ShareLinkId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
