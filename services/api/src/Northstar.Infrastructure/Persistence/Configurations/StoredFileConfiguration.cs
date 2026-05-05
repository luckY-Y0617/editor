using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Files;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class StoredFileConfiguration : IEntityTypeConfiguration<StoredFile>
{
    public void Configure(EntityTypeBuilder<StoredFile> builder)
    {
        builder.ToTable(
            "files",
            table =>
            {
                table.HasCheckConstraint("files_byte_size_check", "byte_size >= 0");
                table.HasCheckConstraint("files_scan_status_check", "scan_status IN ('pending', 'clean', 'blocked', 'failed')");
                table.HasCheckConstraint("files_processing_status_check", "processing_status IN ('pending', 'ready', 'failed')");
            });

        builder.HasKey(file => file.Id);

        builder.Property(file => file.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(file => file.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(file => file.UploadedBy).HasColumnName("uploaded_by");

        builder.Property(file => file.StorageProvider)
            .HasColumnName("storage_provider")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(file => file.Bucket)
            .HasColumnName("bucket")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(file => file.ObjectKey)
            .HasColumnName("object_key")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(file => file.OriginalFilename)
            .HasColumnName("original_filename")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(file => file.MimeType)
            .HasColumnName("mime_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(file => file.ByteSize).HasColumnName("byte_size");
        builder.Property(file => file.ChecksumSha256).HasColumnName("checksum_sha256").HasColumnType("text");
        builder.Property(file => file.Width).HasColumnName("width");
        builder.Property(file => file.Height).HasColumnName("height");

        builder.Property(file => file.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(file => file.ScanStatus)
            .HasColumnName("scan_status")
            .HasColumnType("text")
            .HasDefaultValue(FileScanStatus.Clean)
            .IsRequired();

        builder.Property(file => file.ProcessingStatus)
            .HasColumnName("processing_status")
            .HasColumnType("text")
            .HasDefaultValue(FileProcessingStatus.Ready)
            .IsRequired();

        builder.Property(file => file.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(file => file.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(file => new { file.StorageProvider, file.Bucket, file.ObjectKey })
            .IsUnique()
            .HasDatabaseName("files_storage_object_key");

        builder.HasIndex(file => new { file.WorkspaceId, file.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("files_workspace_created_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(file => file.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(file => file.UploadedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
