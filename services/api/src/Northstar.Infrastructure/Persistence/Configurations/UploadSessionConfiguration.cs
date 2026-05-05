using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Files;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class UploadSessionConfiguration : IEntityTypeConfiguration<UploadSession>
{
    public void Configure(EntityTypeBuilder<UploadSession> builder)
    {
        builder.ToTable(
            "upload_sessions",
            table =>
            {
                table.HasCheckConstraint("upload_sessions_byte_size_check", "byte_size > 0");
                table.HasCheckConstraint("upload_sessions_upload_mode_check", "upload_mode IN ('single', 'multipart')");
                table.HasCheckConstraint("upload_sessions_status_check", "status IN ('initiated', 'uploading', 'completed', 'aborted', 'expired', 'failed', 'finalized')");
            });

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(session => session.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(session => session.OwnerId).HasColumnName("owner_id");

        builder.Property(session => session.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.OriginalFilename)
            .HasColumnName("original_filename")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.MimeType)
            .HasColumnName("mime_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.ByteSize).HasColumnName("byte_size");
        builder.Property(session => session.ChecksumSha256).HasColumnName("checksum_sha256").HasColumnType("text");
        builder.Property(session => session.BizType).HasColumnName("biz_type").HasColumnType("text");

        builder.Property(session => session.StorageProvider)
            .HasColumnName("storage_provider")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.Bucket)
            .HasColumnName("bucket")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.ObjectKey)
            .HasColumnName("object_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(session => session.UploadMode)
            .HasColumnName("upload_mode")
            .HasColumnType("text")
            .HasDefaultValue(UploadMode.Single)
            .IsRequired();

        builder.Property(session => session.MultipartUploadId).HasColumnName("multipart_upload_id").HasColumnType("text");
        builder.Property(session => session.ChunkSize).HasColumnName("chunk_size");
        builder.Property(session => session.TotalParts).HasColumnName("total_parts");

        builder.Property(session => session.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(UploadSessionStatus.Initiated)
            .IsRequired();

        builder.Property(session => session.FinalizedFileId).HasColumnName("finalized_file_id");
        builder.Property(session => session.ExpiresAt).HasColumnName("expires_at");
        builder.Property(session => session.FinalizedAt).HasColumnName("finalized_at");

        builder.Property(session => session.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(session => session.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(session => new { session.WorkspaceId, session.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("upload_sessions_workspace_idempotency_key");

        builder.HasIndex(session => new { session.StorageProvider, session.Bucket, session.ObjectKey })
            .IsUnique()
            .HasDatabaseName("upload_sessions_storage_object_key");

        builder.HasIndex(session => new { session.WorkspaceId, session.OwnerId, session.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("upload_sessions_owner_idx");

        builder.HasIndex(session => new { session.Status, session.ExpiresAt })
            .HasDatabaseName("upload_sessions_status_expires_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(session => session.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(session => session.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(session => session.FinalizedFileId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
