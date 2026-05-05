using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(token => token.UserId).HasColumnName("user_id");

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(token => token.FamilyId).HasColumnName("family_id");
        builder.Property(token => token.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at");
        builder.Property(token => token.RotatedAt).HasColumnName("rotated_at");
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at");
        builder.Property(token => token.ReplacedByTokenId).HasColumnName("replaced_by_token_id");

        builder.Property(token => token.CreatedByIp)
            .HasColumnName("created_by_ip")
            .HasColumnType("text");

        builder.Property(token => token.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("refresh_tokens_token_hash_key");

        builder.HasIndex(token => new { token.UserId, token.FamilyId })
            .HasDatabaseName("refresh_tokens_user_family_idx");

        builder.HasIndex(token => token.ExpiresAt)
            .HasDatabaseName("refresh_tokens_expires_idx");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<RefreshToken>()
            .WithMany()
            .HasForeignKey(token => token.ReplacedByTokenId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
