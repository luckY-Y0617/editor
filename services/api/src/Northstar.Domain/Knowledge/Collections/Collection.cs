using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Collections;

public sealed class Collection
{
    private Collection()
    {
        Title = string.Empty;
    }

    public Collection(
        Guid workspaceId,
        Guid spaceId,
        string title,
        Guid? parentCollectionId = null,
        Guid? createdBy = null,
        Guid? id = null,
        string? slug = null,
        decimal sortOrder = 0m)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        ParentCollectionId = parentCollectionId;
        Title = Required(title, nameof(title));
        Slug = slug;
        SortOrder = sortOrder;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid? ParentCollectionId { get; private set; }
    public string Title { get; private set; }
    public string? Slug { get; private set; }
    public decimal SortOrder { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void Rename(string title)
    {
        Title = Required(title, nameof(title));
        Touch();
    }

    public void SetSortOrder(decimal sortOrder)
    {
        SortOrder = sortOrder;
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
