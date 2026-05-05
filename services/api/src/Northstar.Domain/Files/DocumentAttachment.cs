using Northstar.Domain.Shared;

namespace Northstar.Domain.Files;

public sealed class DocumentAttachment
{
    private DocumentAttachment()
    {
        RelationType = DocumentAttachmentRelationType.Attachment;
        Metadata = "{}";
    }

    public DocumentAttachment(
        Guid workspaceId,
        Guid documentId,
        Guid fileId,
        string relationType,
        string? metadata = null,
        Guid? createdBy = null,
        Guid? id = null)
    {
        if (!DocumentAttachmentRelationType.IsValid(relationType))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "relationType is invalid.");
        }

        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        DocumentId = documentId;
        FileId = fileId;
        RelationType = relationType;
        Metadata = string.IsNullOrWhiteSpace(metadata) ? "{}" : metadata;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid DocumentId { get; private set; }
    public Guid FileId { get; private set; }
    public string RelationType { get; private set; }
    public string Metadata { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
