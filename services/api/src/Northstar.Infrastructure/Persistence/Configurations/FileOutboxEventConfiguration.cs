using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Files;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class FileOutboxEventConfiguration : IEntityTypeConfiguration<FileOutboxEvent>
{
    public void Configure(EntityTypeBuilder<FileOutboxEvent> builder)
    {
        builder.ToTable(
            "file_outbox_events",
            table =>
            {
                table.HasCheckConstraint("file_outbox_events_status_check", "status IN ('pending', 'published', 'failed')");
                table.HasCheckConstraint("file_outbox_events_retry_count_check", "retry_count >= 0");
            });

        builder.HasKey(outboxEvent => outboxEvent.Id);

        builder.Property(outboxEvent => outboxEvent.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(outboxEvent => outboxEvent.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(outboxEvent => outboxEvent.AggregateType)
            .HasColumnName("aggregate_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(outboxEvent => outboxEvent.AggregateId).HasColumnName("aggregate_id");

        builder.Property(outboxEvent => outboxEvent.EventType)
            .HasColumnName("event_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(outboxEvent => outboxEvent.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(outboxEvent => outboxEvent.Headers)
            .HasColumnName("headers")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(outboxEvent => outboxEvent.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(FileOutboxEventStatus.Pending)
            .IsRequired();

        builder.Property(outboxEvent => outboxEvent.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0);

        builder.Property(outboxEvent => outboxEvent.NextRetryAt)
            .HasColumnName("next_retry_at")
            .HasDefaultValueSql("now()");

        builder.Property(outboxEvent => outboxEvent.LastError)
            .HasColumnName("last_error")
            .HasColumnType("text");

        builder.Property(outboxEvent => outboxEvent.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(outboxEvent => outboxEvent.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(outboxEvent => new { outboxEvent.Status, outboxEvent.NextRetryAt, outboxEvent.CreatedAt })
            .HasDatabaseName("file_outbox_events_dispatch_idx");

        builder.HasIndex(outboxEvent => new { outboxEvent.WorkspaceId, outboxEvent.AggregateType, outboxEvent.AggregateId })
            .HasDatabaseName("file_outbox_events_aggregate_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(outboxEvent => outboxEvent.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
