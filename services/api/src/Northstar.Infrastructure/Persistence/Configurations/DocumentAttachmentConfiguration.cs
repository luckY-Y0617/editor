using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Files;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentAttachmentConfiguration : IEntityTypeConfiguration<DocumentAttachment>
{
    public void Configure(EntityTypeBuilder<DocumentAttachment> builder)
    {
        builder.ToTable(
            "document_attachments",
            table =>
            {
                table.HasCheckConstraint("document_attachments_relation_type_check", "relation_type IN ('attachment', 'inline_image', 'cover')");
            });

        builder.HasKey(attachment => attachment.Id);

        builder.Property(attachment => attachment.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(attachment => attachment.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(attachment => attachment.DocumentId).HasColumnName("document_id");
        builder.Property(attachment => attachment.FileId).HasColumnName("file_id");

        builder.Property(attachment => attachment.RelationType)
            .HasColumnName("relation_type")
            .HasColumnType("text")
            .HasDefaultValue(DocumentAttachmentRelationType.Attachment)
            .IsRequired();

        builder.Property(attachment => attachment.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(attachment => attachment.CreatedBy).HasColumnName("created_by");

        builder.Property(attachment => attachment.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(attachment => new { attachment.WorkspaceId, attachment.DocumentId, attachment.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("document_attachments_document_idx");

        builder.HasIndex(attachment => new { attachment.WorkspaceId, attachment.FileId })
            .HasDatabaseName("document_attachments_file_idx");

        builder.HasIndex(attachment => new { attachment.DocumentId, attachment.FileId, attachment.RelationType })
            .IsUnique()
            .HasDatabaseName("document_attachments_document_file_relation_key");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(attachment => attachment.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(attachment => attachment.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<StoredFile>()
            .WithMany()
            .HasForeignKey(attachment => attachment.FileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(attachment => attachment.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
