namespace Northstar.Infrastructure.Search;

public sealed class DocumentSearchIndex
{
    private DocumentSearchIndex()
    {
        Title = string.Empty;
        TextContent = string.Empty;
    }

    public DocumentSearchIndex(
        Guid documentId,
        Guid workspaceId,
        Guid spaceId,
        string title,
        string textContent)
    {
        DocumentId = documentId;
        WorkspaceId = workspaceId;
        SpaceId = spaceId;
        Title = title;
        TextContent = textContent;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid DocumentId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SpaceId { get; private set; }
    public string Title { get; private set; }
    public string TextContent { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void Update(string title, string textContent, Guid spaceId)
    {
        Title = title;
        TextContent = textContent;
        SpaceId = spaceId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

