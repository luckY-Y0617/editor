using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Spaces;

public sealed class Space
{
    private Space()
    {
        Name = string.Empty;
        Slug = string.Empty;
        Visibility = SpaceVisibility.Workspace;
    }

    public Space(
        Guid workspaceId,
        string name,
        string slug,
        Guid? createdBy = null,
        Guid? id = null,
        decimal sortOrder = 0m)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        Name = Required(name, nameof(name));
        Slug = Required(slug, nameof(slug));
        Visibility = SpaceVisibility.Workspace;
        SortOrder = sortOrder;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string? Description { get; private set; }
    public string Visibility { get; private set; }
    public decimal SortOrder { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void UpdateDetails(string name, string? description)
    {
        Name = Required(name, nameof(name));
        Description = description;
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
}
