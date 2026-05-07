using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northstar.Domain.Organizations;

namespace Northstar.Infrastructure.Persistence.Configurations;

public sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.ToTable("organizations");

        builder.HasKey(organization => organization.Id);

        builder.Property(organization => organization.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()");

        builder.Property(organization => organization.Name)
            .HasColumnName("name")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(organization => organization.Slug)
            .HasColumnName("slug")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(organization => organization.Status)
            .HasColumnName("status")
            .HasColumnType("text")
            .HasDefaultValue(OrganizationStatus.Active)
            .IsRequired();

        builder.Property(organization => organization.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()");

        builder.Property(organization => organization.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("now()");

        builder.Property(organization => organization.DeletedAt).HasColumnName("deleted_at");

        builder.HasIndex(organization => organization.Slug)
            .IsUnique()
            .HasDatabaseName("organizations_slug_key");
    }
}
