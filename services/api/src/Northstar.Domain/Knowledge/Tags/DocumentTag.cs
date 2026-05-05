namespace Northstar.Domain.Knowledge.Tags;

public sealed class DocumentTag
{
    private DocumentTag()
    {
    }

    public DocumentTag(Guid workspaceId, Guid documentId, Guid tagId)
    {
        WorkspaceId = workspaceId;
        DocumentId = documentId;
        TagId = tagId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid WorkspaceId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid TagId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

