using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class ScimTokenConfiguration : IEntityTypeConfiguration<ScimToken>
{
    public void Configure(EntityTypeBuilder<ScimToken> builder)
    {
        builder.ToTable("scim_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(token => token.WorkspaceId).HasColumnName("workspace_id");

        builder.Property(token => token.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasColumnName("token_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(token => token.CreatedBy).HasColumnName("created_by");

        builder.Property(token => token.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(token => token.ExpiresAt).HasColumnName("expires_at");
        builder.Property(token => token.RevokedAt).HasColumnName("revoked_at");
        builder.Property(token => token.LastUsedAt).HasColumnName("last_used_at");

        builder.HasIndex(token => token.TokenHash)
            .IsUnique()
            .HasDatabaseName("idx_scim_tokens_token_hash");

        builder.HasIndex(token => new { token.WorkspaceId, token.RevokedAt, token.ExpiresAt })
            .HasDatabaseName("idx_scim_tokens_workspace_active");

        builder.HasOne<Workspace>()
            .WithMany()
            .HasForeignKey(token => token.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(token => token.CreatedBy)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
