using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ShareLinkAccessEventConfiguration : IEntityTypeConfiguration<ShareLinkAccessEvent>
{
    public void Configure(EntityTypeBuilder<ShareLinkAccessEvent> builder)
    {
        builder.ToTable(
            "share_link_access_events",
            table =>
            {
                table.HasCheckConstraint("share_link_access_events_resource_type_check", "resource_type IN ('library', 'collection', 'document')");
                table.HasCheckConstraint("share_link_access_events_audience_check", "audience IN ('workspace', 'external', 'public')");
                table.HasCheckConstraint("share_link_access_events_event_type_check", "event_type IN ('resolve', 'access', 'download')");
                table.HasCheckConstraint("share_link_access_events_result_check", "result IN ('success', 'fail')");
            });

        builder.HasKey(accessEvent => accessEvent.Id);

        builder.Property(accessEvent => accessEvent.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(accessEvent => accessEvent.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(accessEvent => accessEvent.ShareLinkId).HasColumnName("share_link_id");

        builder.Property(accessEvent => accessEvent.ResourceType)
            .HasColumnName("resource_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(accessEvent => accessEvent.ResourceId).HasColumnName("resource_id");
        builder.Property(accessEvent => accessEvent.ActorUserId).HasColumnName("actor_user_id");

        builder.Property(accessEvent => accessEvent.Audience)
            .HasColumnName("audience")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(accessEvent => accessEvent.EventType)
            .HasColumnName("event_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(accessEvent => accessEvent.Result)
            .HasColumnName("result")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(accessEvent => accessEvent.FailureCategory)
            .HasColumnName("failure_category")
            .HasColumnType("text");

        builder.Property(accessEvent => accessEvent.RemoteIp)
            .HasColumnName("remote_ip")
            .HasColumnType("text");

        builder.Property(accessEvent => accessEvent.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(accessEvent => accessEvent.OccurredAt)
            .HasColumnName("occurred_at")
            .HasDefaultValueSql("now()");

        builder.Property(accessEvent => accessEvent.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.HasIndex(accessEvent => new { accessEvent.WorkspaceId, accessEvent.ShareLinkId, accessEvent.OccurredAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_share_link_access_events_link_time");

        builder.HasIndex(accessEvent => new { accessEvent.WorkspaceId, accessEvent.ResourceType, accessEvent.ResourceId, accessEvent.OccurredAt })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("idx_share_link_access_events_resource_time");

        builder.HasIndex(accessEvent => new { accessEvent.WorkspaceId, accessEvent.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_share_link_access_events_workspace_time");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(accessEvent => accessEvent.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<ShareLink>()
            .WithMany()
            .HasForeignKey(accessEvent => accessEvent.ShareLinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(accessEvent => accessEvent.ActorUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
