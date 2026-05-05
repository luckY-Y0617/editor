using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class UserCredentialConfiguration : IEntityTypeConfiguration<UserCredential>
{
    public void Configure(EntityTypeBuilder<UserCredential> builder)
    {
        builder.ToTable("user_credentials");

        builder.HasKey(credential => credential.UserId);

        builder.Property(credential => credential.UserId).HasColumnName("user_id");

        builder.Property(credential => credential.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(credential => credential.PasswordHashAlgorithm)
            .HasColumnName("password_hash_algorithm")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(credential => credential.PasswordUpdatedAt)
            .HasColumnName("password_updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(credential => credential.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(credential => credential.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.HasOne<User>()
            .WithOne()
            .HasForeignKey<UserCredential>(credential => credential.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
