using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Search;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentSearchIndexConfiguration : IEntityTypeConfiguration<DocumentSearchIndex>
{
    public void Configure(EntityTypeBuilder<DocumentSearchIndex> builder)
    {
        builder.ToTable("document_search_index");

        builder.HasKey(index => index.DocumentId);

        builder.Property(index => index.DocumentId).HasColumnName("document_id");
        builder.Property(index => index.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(index => index.SpaceId).HasColumnName("space_id");

        builder.Property(index => index.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(index => index.TextContent)
            .HasColumnName("text_content")
            .HasColumnType("text")
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(index => index.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(index => new { index.WorkspaceId, index.SpaceId })
            .HasDatabaseName("document_search_workspace_idx");

        builder.HasOne<Document>()
            .WithOne()
            .HasForeignKey<DocumentSearchIndex>(index => index.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(index => index.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(index => index.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

