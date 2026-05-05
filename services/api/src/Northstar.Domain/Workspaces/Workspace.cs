using Northstar.Domain.Shared;

namespace Northstar.Domain.Workspaces;

public sealed class Workspace
{
    private Workspace()
    {
        Name = string.Empty;
        Slug = string.Empty;
    }

    public Workspace(string name, string slug, Guid? createdBy = null, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Name = Required(name, nameof(name));
        Slug = Required(slug, nameof(slug));
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public Guid? DefaultSpaceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void SetDefaultSpace(Guid spaceId)
    {
        DefaultSpaceId = spaceId;
        Touch();
    }

    public void Rename(string name)
    {
        Name = Required(name, nameof(name));
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
