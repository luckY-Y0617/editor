using Northstar.Domain.Shared;

namespace Northstar.Domain.Organizations;

public sealed class Organization
{
    private Organization()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Status = OrganizationStatus.Active;
    }

    public Organization(string name, string slug, Guid? id = null, string status = OrganizationStatus.Active)
    {
        Id = id ?? Guid.NewGuid();
        Name = Required(name, nameof(name));
        Slug = Required(slug, nameof(slug));
        Status = ValidStatus(status);
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void UpdateProfile(string name, string slug)
    {
        Name = Required(name, nameof(name));
        Slug = Required(slug, nameof(slug));
        Touch();
    }

    private void Touch()
    {
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }

    private static string ValidStatus(string status)
    {
        var normalized = Required(status, nameof(status)).ToLowerInvariant();
        return OrganizationStatus.IsValid(normalized)
            ? normalized
            : throw new DomainException(DomainErrorCodes.ValidationError, "organization status is invalid.");
    }
}
