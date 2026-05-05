using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Tags;

public sealed class Tag
{
    private Tag()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public Tag(Guid workspaceId, string name, string slug, Guid? createdBy = null, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        Name = Required(name, nameof(name));
        Slug = Required(slug, nameof(slug));
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Color { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }
}

