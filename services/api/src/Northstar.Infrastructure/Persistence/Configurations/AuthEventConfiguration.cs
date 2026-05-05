using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class AuthEventConfiguration : IEntityTypeConfiguration<AuthEvent>
{
    public void Configure(EntityTypeBuilder<AuthEvent> builder)
    {
        builder.ToTable("auth_events");

        builder.HasKey(authEvent => authEvent.Id);

        builder.Property(authEvent => authEvent.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(authEvent => authEvent.UserId).HasColumnName("user_id");

        builder.Property(authEvent => authEvent.Action)
            .HasColumnName("action")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(authEvent => authEvent.Succeeded).HasColumnName("succeeded");

        builder.Property(authEvent => authEvent.IpAddress)
            .HasColumnName("ip_address")
            .HasColumnType("text");

        builder.Property(authEvent => authEvent.UserAgent)
            .HasColumnName("user_agent")
            .HasColumnType("text");

        builder.Property(authEvent => authEvent.Metadata)
            .HasColumnName("metadata")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(authEvent => authEvent.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.HasIndex(authEvent => new { authEvent.UserId, authEvent.CreatedAt })
            .IsDescending(false, true)
            .HasDatabaseName("auth_events_user_idx");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(authEvent => authEvent.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
