using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Activity;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ActivityEventConfiguration : IEntityTypeConfiguration<ActivityEvent>
{
    public void Configure(EntityTypeBuilder<ActivityEvent> builder)
    {
        builder.ToTable("activity_events");

        builder.HasKey(activity => activity.Id);

        builder.Property(activity => activity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(activity => activity.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(activity => activity.ActorId).HasColumnName("actor_id");

        builder.Property(activity => activity.EntityType)
            .HasColumnName("entity_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(activity => activity.EntityId).HasColumnName("entity_id");

        builder.Property(activity => activity.Action)
            .HasColumnName("action")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(activity => activity.Summary)
            .HasColumnName("summary")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(activity => activity.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(activity => activity.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(activity => new { activity.WorkspaceId, activity.EntityType, activity.EntityId, activity.CreatedAt })
            .IsDescending(false, false, false, true)
            .HasDatabaseName("activity_events_entity_idx");

        builder.HasIndex(activity => new { activity.WorkspaceId, activity.ActorId, activity.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("activity_events_actor_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(activity => activity.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(activity => activity.ActorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

