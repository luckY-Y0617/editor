using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Collections;
using Northstar.Domain.Knowledge.Spaces;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> builder)
    {
        builder.ToTable("collections");

        builder.HasKey(collection => collection.Id);

        builder.Property(collection => collection.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(collection => collection.WorkspaceId).HasColumnName("workspace_id");
        builder.Property(collection => collection.SpaceId).HasColumnName("space_id");
        builder.Property(collection => collection.ParentCollectionId).HasColumnName("parent_collection_id");

        builder.Property(collection => collection.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(collection => collection.Slug)
            .HasColumnName("slug")
            .HasColumnType("text");

        builder.Property(collection => collection.SortOrder)
            .HasColumnName("sort_order")
            .HasColumnType("numeric(20,10)")
            .HasDefaultValue(0m);

        builder.Property(collection => collection.CreatedBy).HasColumnName("created_by");

        builder.Property(collection => collection.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(collection => collection.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(collection => collection.ArchivedAt).HasColumnName("archived_at");
        builder.Property(collection => collection.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(collection => new
            {
                collection.WorkspaceId,
                collection.SpaceId,
                collection.ParentCollectionId,
                collection.SortOrder
            })
            .HasDatabaseName("collections_space_order_idx");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(collection => collection.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Space>()
            .WithMany()
            .HasForeignKey(collection => collection.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Collection>()
            .WithMany()
            .HasForeignKey(collection => collection.ParentCollectionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(collection => collection.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
