using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable(
            "documents",
            table =>
            {
                table.HasCheckConstraint("documents_status_check", "status IN ('draft', 'published', 'archived')");
                table.HasCheckConstraint("documents_revision_check", "revision >= 0");
            });

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(document => document.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(document => document.SpaceId).HasColumnName("space_id");
        builder.Property(document => document.CollectionId).HasColumnName("collection_id");
        builder.Property(document => document.OwnerId).HasColumnName("owner_id");

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(document => document.Slug)
            .HasColumnName("slug")
            .HasColumnType("text");

        builder.Property(document => document.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(DocumentStatus.Draft)
            .IsRequired();

        builder.Property(document => document.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("numeric(20,10)")
            .HasDefaultValue(0m);

        builder.Property(document => document.Revision)
            .HasColumnName("revision")
            .HasDefaultValue(0L);

        builder.Property(document => document.CurrentPublishedVersionId).HasColumnName("current_published_version_id");
        builder.Property(document => document.LastEditedBy).HasColumnName("last_edited_by");
        builder.Property(document => document.CreatedBy).HasColumnName("created_by");

        builder.Property(document => document.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(document => document.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(document => document.PublishedAt).HasColumnName("published_at");
        builder.Property(document => document.ArchivedAt).HasColumnName("archived_at");
        builder.Property(document => document.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(document => new
            {
                document.WorkspaceId,
                document.SpaceId,
                document.CollectionId,
                document.SortOrder
            })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("documents_collection_order_idx");

        builder.HasIndex(document => new { document.WorkspaceId, document.UpdatedAt })
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("documents_updated_idx")
            .IsDescending(false, true);

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(document => document.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(document => document.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Collection>()
            .WithMany()
            .HasForeignKey(document => document.CollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => document.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => document.LastEditedBy)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(document => document.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
