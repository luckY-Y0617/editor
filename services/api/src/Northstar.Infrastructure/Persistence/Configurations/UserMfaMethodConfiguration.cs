using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class UserMfaMethodConfiguration : IEntityTypeConfiguration<UserMfaMethod>
{
    public void Configure(EntityTypeBuilder<UserMfaMethod> builder)
    {
        builder.ToTable("user_mfa_methods", table =>
        {
            table.HasCheckConstraint("user_mfa_methods_method_type_check", "method_type IN ('totp')");
            table.HasCheckConstraint("user_mfa_methods_status_check", "status IN ('pending', 'enabled', 'disabled')");
        });

        builder.HasKey(method => method.Id);

        builder.Property(method => method.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(method => method.UserId).HasColumnName("user_id");

        builder.Property(method => method.MethodType)
            .HasColumnName("method_type")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(method => method.SecretCiphertext)
            .HasColumnName("secret_ciphertext")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(method => method.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(MfaMethodStatuses.Pending)
            .IsRequired();

        builder.Property(method => method.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(method => method.VerifiedAt).HasColumnName("verified_at");
        builder.Property(method => method.DisabledAt).HasColumnName("disabled_at");
        builder.Property(method => method.LastUsedAt).HasColumnName("last_used_at");

        builder.HasIndex(method => new { method.UserId, method.MethodType })
            .IsUnique()
            .HasFilter("status IN ('pending', 'enabled')")
            .HasDatabaseName("idx_user_mfa_methods_active_type");

        builder.HasIndex(method => new { method.UserId, method.Status, method.CreatedAt })
            .IsDescending(false, false, true)
            .HasDatabaseName("idx_user_mfa_methods_user_status");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(method => method.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
