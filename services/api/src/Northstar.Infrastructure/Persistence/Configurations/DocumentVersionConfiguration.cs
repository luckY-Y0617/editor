using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Versions;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable(
            "document_versions",
            table =>
            {
                table.HasCheckConstraint("document_versions_version_no_check", "version_no > 0");
                table.HasCheckConstraint("document_versions_word_count_check", "word_count >= 0");
                table.HasCheckConstraint("document_versions_version_type_check", "version_type IN ('manual', 'published', 'imported', 'system')");
            });

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(version => version.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(version => version.DocumentId).HasColumnName("document_id");
        builder.Property(version => version.VersionNo).HasColumnName("version_no");

        builder.Property(version => version.Label)
            .HasColumnName("label")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(version => version.VersionType)
            .HasColumnName("version_type")
            .HasColumnType("text")
            .HasDefaultValue(DocumentVersionType.System)
            .IsRequired();

        builder.Property(version => version.Content)
            .HasColumnName("content")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(version => version.TextContent)
            .HasColumnName("text_content")
            .HasColumnType("text")
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(version => version.Outline)
            .HasColumnName("outline")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        builder.Property(version => version.WordCount)
            .HasColumnName("word_count")
            .HasDefaultValue(0);

        builder.Property(version => version.CreatedBy).HasColumnName("created_by");

        builder.Property(version => version.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(version => version.PublishedAt).HasColumnName("published_at");

        builder.HasIndex(version => new { version.DocumentId, version.VersionNo })
            .IsUnique()
            .HasDatabaseName("document_versions_document_version_no_key");

        builder.HasIndex(version => new { version.DocumentId, version.Label })
            .IsUnique()
            .HasDatabaseName("document_versions_document_label_key");

        builder.HasIndex(version => new { version.WorkspaceId, version.DocumentId, version.VersionNo })
            .IsDescending(false, false, true)
            .HasDatabaseName("document_versions_doc_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(version => version.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(version => version.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(version => version.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

