using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Comments;
using Northstar.Domain.Knowledge.Documents;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class CommentThreadConfiguration : IEntityTypeConfiguration<CommentThread>
{
    public void Configure(EntityTypeBuilder<CommentThread> builder)
    {
        builder.ToTable(
            "comment_threads",
            table =>
            {
                table.HasCheckConstraint("comment_threads_status_check", "status IN ('open', 'resolved')");
            });

        builder.HasKey(thread => thread.Id);

        builder.Property(thread => thread.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(thread => thread.DocumentId).HasColumnName("document_id");

        builder.Property(thread => thread.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(CommentThreadStatus.Open)
            .IsRequired();

        builder.Property(thread => thread.AnchorJson)
            .HasColumnName("anchor")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(thread => thread.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(thread => thread.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(thread => thread.ResolvedAt).HasColumnName("resolved_at");

        builder.HasIndex(thread => new { thread.DocumentId, thread.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("comment_threads_document_idx");

        builder.HasOne<Document>()
            .WithMany()
            .HasForeignKey(thread => thread.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
