using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasColumnType("citext");

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(user => user.AvatarUrl).HasColumnName("avatar_url").HasColumnType("text");
        builder.Property(user => user.ExternalProvider).HasColumnName("external_provider").HasColumnType("text");
        builder.Property(user => user.ExternalSubjectId).HasColumnName("external_subject_id").HasColumnType("text");

        builder.Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(user => user.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(user => user.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("users_email_key");

        builder.HasIndex(user => new { user.ExternalProvider, user.ExternalSubjectId })
            .IsUnique()
            .HasFilter("external_provider IS NOT NULL AND external_subject_id IS NOT NULL")
            .HasDatabaseName("users_external_provider_subject_key");
    }
}
