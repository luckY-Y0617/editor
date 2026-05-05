namespace Northstar.Domain.Knowledge.Links;

public sealed class DocumentLink
{
    private DocumentLink()
    {
        LinkType = DocumentLinkType.Reference;
    }

    public DocumentLink(
        Guid workspaceId,
        Guid sourceDocumentId,
        Guid? targetDocumentId,
        string? targetUrl,
        string linkType,
        string? anchorText = null,
        string? sourceAnchor = null,
        string? targetAnchor = null,
        Guid? createdBy = null,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        SourceDocumentId = sourceDocumentId;
        TargetDocumentId = targetDocumentId;
        TargetUrl = targetUrl;
        LinkType = linkType;
        AnchorText = anchorText;
        SourceAnchor = sourceAnchor;
        TargetAnchor = targetAnchor;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid SourceDocumentId { get; private set; }
    public Guid? TargetDocumentId { get; private set; }
    public string? TargetUrl { get; private set; }
    public string LinkType { get; private set; }
    public string? AnchorText { get; private set; }
    public string? SourceAnchor { get; private set; }
    public string? TargetAnchor { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

