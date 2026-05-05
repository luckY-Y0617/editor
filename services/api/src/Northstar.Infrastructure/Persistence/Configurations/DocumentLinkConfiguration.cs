using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Links;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentLinkConfiguration : IEntityTypeConfiguration<DocumentLink>
{
    public void Configure(EntityTypeBuilder<DocumentLink> builder)
    {
        builder.ToTable(
            "document_links",
            table =>
            {
                table.HasCheckConstraint("document_links_link_type_check", "link_type IN ('reference', 'related', 'embed', 'external')");
                table.HasCheckConstraint("document_links_target_check", "target_document_id IS NOT NULL OR target_url IS NOT NULL");
            });

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(link => link.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(link => link.SourceDocumentId).HasColumnName("source_document_id");
        builder.Property(link => link.TargetDocumentId).HasColumnName("target_document_id");

        builder.Property(link => link.TargetUrl)
            .HasColumnName("target_url")
            .HasColumnType("text");

        builder.Property(link => link.LinkType)
            .HasColumnName("link_type")
            .HasColumnType("text")
            .HasDefaultValue(DocumentLinkType.Reference)
            .IsRequired();

        builder.Property(link => link.AnchorText)
            .HasColumnName("anchor_text")
            .HasColumnType("text");

        builder.Property(link => link.SourceAnchor)
            .HasColumnName("source_anchor")
            .HasColumnType("jsonb");

        builder.Property(link => link.TargetAnchor)
            .HasColumnName("target_anchor")
            .HasColumnType("jsonb");

        builder.Property(link => link.CreatedBy).HasColumnName("created_by");

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(link => new { link.WorkspaceId, link.SourceDocumentId, link.LinkType })
            .HasDatabaseName("document_links_source_idx");

        builder.HasIndex(link => new { link.WorkspaceId, link.TargetDocumentId })
            .HasFilter("target_document_id IS NOT NULL")
            .HasDatabaseName("document_links_target_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(link => link.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(link => link.SourceDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(link => link.TargetDocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(link => link.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

