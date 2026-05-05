using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Knowledge.Comments;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class CommentMessageConfiguration : IEntityTypeConfiguration<CommentMessage>
{
    public void Configure(EntityTypeBuilder<CommentMessage> builder)
    {
        builder.ToTable("comment_messages");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(message => message.ThreadId).HasColumnName("thread_id");

        builder.Property(message => message.Body)
            .HasColumnName("body")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(message => message.AuthorUserId).HasColumnName("author_user_id");

        builder.Property(message => message.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(message => message.UpdatedAt).HasColumnName("updated_at");
        builder.Property(message => message.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(message => new { message.ThreadId, message.CreatedAt })
            .HasDatabaseName("comment_messages_thread_idx");

        builder.HasOne<CommentThread>()
            .WithMany()
            .HasForeignKey(message => message.ThreadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.AuthorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
