using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentDraftConfiguration : IEntityTypeConfiguration<DocumentDraft>
{
    public void Configure(EntityTypeBuilder<DocumentDraft> builder)
    {
        builder.ToTable(
            "document_drafts",
            table => table.HasCheckConstraint("document_drafts_word_count_check", "word_count >= 0"));

        builder.HasKey(draft => draft.DocumentId);

        builder.Property(draft => draft.DocumentId).HasColumnName("document_id");
        builder.Property(draft => draft.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(draft => draft.Content)
            .HasColumnName("content")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(draft => draft.TextContent)
            .HasColumnName("text_content")
            .HasColumnType("text")
            .HasDefaultValue(string.Empty)
            .IsRequired();

        builder.Property(draft => draft.Outline)
            .HasColumnName("outline")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'[]'::jsonb")
            .IsRequired();

        builder.Property(draft => draft.WordCount)
            .HasColumnName("word_count")
            .HasDefaultValue(0);

        builder.Property(draft => draft.ContentHash)
            .HasColumnName("content_hash")
            .HasColumnType("text");

        builder.Property(draft => draft.UpdatedBy).HasColumnName("updated_by");

        builder.Property(draft => draft.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(draft => new { draft.WorkspaceId, draft.UpdatedAt })
            .HasDatabaseName("document_drafts_workspace_idx")
            .IsDescending(false, true);

        builder.HasOne<Document>()
            .WithOne()
            .HasForeignKey<DocumentDraft>(draft => draft.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(draft => draft.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(draft => draft.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
