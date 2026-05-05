using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Documents;

public sealed class Document
{
    private Document()
    {
        Title = string.Empty;
        Status = DocumentStatus.Draft;
    }

    public Document(
        Guid workspaceId,
        Guid spaceId,
        Guid? collectionId,
        string title,
        Guid? ownerId = null,
        Guid? createdBy = null,
        Guid? id = null,
        string? slug = null,
        decimal sortOrder = 0m)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        CollectionId = collectionId;
        OwnerId = ownerId;
        Title = Required(title, nameof(title));
        Slug = slug;
        Status = DocumentStatus.Draft;
        SortOrder = sortOrder;
        CreatedBy = createdBy;
        LastEditedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public Guid? CollectionId { get; private set; }
    public Guid? OwnerId { get; private set; }
    public string Title { get; private set; }
    public string? Slug { get; private set; }
    public string Status { get; private set; }
    public decimal SortOrder { get; private set; }
    public long Revision { get; private set; }
    public Guid? CurrentPublishedVersionId { get; private set; }
    public Guid? LastEditedBy { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public void Rename(string title, Guid? editedBy = null)
    {
        Title = Required(title, nameof(title));
        MarkEdited(editedBy);
    }

    public void Move(Guid? collectionId, decimal? sortOrder = null, Guid? editedBy = null)
    {
        CollectionId = collectionId;
        if (sortOrder.HasValue)
        {
            SortOrder = sortOrder.Value;
        }

        MarkEdited(editedBy);
    }

    public void IncrementRevision(Guid? editedBy = null)
    {
        Revision++;
        MarkEdited(editedBy);
    }

    public bool Archive(Guid? editedBy = null)
    {
        EnsureNotDeleted("Deleted documents cannot be archived.");

        if (Status == DocumentStatus.Archived)
        {
            return false;
        }

        Status = DocumentStatus.Archived;
        ArchivedAt = DateTimeOffset.UtcNow;
        MarkEdited(editedBy);
        return true;
    }

    public bool Restore(Guid? editedBy = null)
    {
        EnsureNotDeleted("Deleted documents cannot be restored.");

        if (Status != DocumentStatus.Archived)
        {
            return false;
        }

        Status = DocumentStatus.Draft;
        ArchivedAt = null;
        MarkEdited(editedBy);
        return true;
    }

    public bool Delete(Guid? editedBy = null)
    {
        if (DeletedAt.HasValue)
        {
            return false;
        }

        DeletedAt = DateTimeOffset.UtcNow;
        MarkEdited(editedBy);
        return true;
    }

    private void MarkEdited(Guid? editedBy)
    {
        LastEditedBy = editedBy ?? LastEditedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private void EnsureNotDeleted(string message)
    {
        if (DeletedAt.HasValue)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, message);
        }
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
