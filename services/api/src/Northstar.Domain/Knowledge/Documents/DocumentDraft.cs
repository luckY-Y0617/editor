using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Documents;

public sealed class DocumentDraft
{
    private DocumentDraft()
    {
        Content = JsonDefaults.EmptyTiptapDocument;
        TextContent = string.Empty;
        Outline = JsonDefaults.EmptyArray;
    }

    public DocumentDraft(Guid documentId, Guid workspaceId, string? content = null, Guid? updatedBy = null)
    {
        DocumentId = documentId;
        WorkspaceId = workspaceId;
        Content = string.IsNullOrWhiteSpace(content) ? JsonDefaults.EmptyTiptapDocument : content;
        TextContent = string.Empty;
        Outline = JsonDefaults.EmptyArray;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public Guid DocumentId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public string Content { get; private set; }
    public string TextContent { get; private set; }
    public string Outline { get; private set; }
    public int WordCount { get; private set; }
    public string? ContentHash { get; private set; }
    public Guid? UpdatedBy { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public void UpdateContent(
        string content,
        string textContent,
        string outline,
        int wordCount,
        string? contentHash,
        Guid? updatedBy = null)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "content is required.");
        }

        if (wordCount < 0)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "wordCount cannot be negative.");
        }

        Content = content;
        TextContent = textContent;
        Outline = string.IsNullOrWhiteSpace(outline) ? JsonDefaults.EmptyArray : outline;
        WordCount = wordCount;
        ContentHash = contentHash;
        UpdatedBy = updatedBy;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

