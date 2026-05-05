using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Tags;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentTagConfiguration : IEntityTypeConfiguration<DocumentTag>
{
    public void Configure(EntityTypeBuilder<DocumentTag> builder)
    {
        builder.ToTable("document_tags");

        builder.HasKey(documentTag => new { documentTag.DocumentId, documentTag.TagId });

        builder.Property(documentTag => documentTag.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(documentTag => documentTag.DocumentId).HasColumnName("document_id");
        builder.Property(documentTag => documentTag.TagId).HasColumnName("tag_id");

        builder.Property(documentTag => documentTag.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(documentTag => new { documentTag.WorkspaceId, documentTag.TagId })
            .HasDatabaseName("document_tags_tag_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(documentTag => documentTag.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(documentTag => documentTag.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tag>()
            .WithMany()
            .HasForeignKey(documentTag => documentTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

