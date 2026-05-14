using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Versions;

public sealed class DocumentVersion
{
    private DocumentVersion()
    {
        Label = string.Empty;
        VersionType = DocumentVersionType.System;
        Content = JsonDefaults.EmptyTiptapDocument;
        TextContent = string.Empty;
        Outline = JsonDefaults.EmptyArray;
    }

    public DocumentVersion(
        Guid workspaceId,
        Guid documentId,
        int versionNo,
        string label,
        string versionType,
        string content,
        string textContent,
        string outline,
        int wordCount,
        Guid? createdBy = null,
        Guid? id = null)
    {
        if (versionNo <= 0)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "versionNo must be positive.");
        }

        if (wordCount < 0)
        {
            throw new DomainException(DomainErrorCodes.ValidationError, "wordCount cannot be negative.");
        }

        Id = id ?? Guid.NewGuid();
        WorkspaceId = workspaceId;
        DocumentId = documentId;
        VersionNo = versionNo;
        Label = Required(label, nameof(label));
        VersionType = Required(versionType, nameof(versionType));
        Content = Required(content, nameof(content));
        TextContent = textContent;
        Outline = string.IsNullOrWhiteSpace(outline) ? JsonDefaults.EmptyArray : outline;
        WordCount = wordCount;
        CreatedBy = createdBy;
        CreatedAt = DateTimeOffset.UtcNow;
        PublishedAt = VersionType == DocumentVersionType.Published ? CreatedAt : null;
    }

    public Guid Id { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid DocumentId { get; private set; }
    public int VersionNo { get; private set; }
    public string Label { get; private set; }
    public string VersionType { get; private set; }
    public string Content { get; private set; }
    public string TextContent { get; private set; }
    public string Outline { get; private set; }
    public int WordCount { get; private set; }
    public Guid? CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? PublishedAt { get; private set; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value.Trim();
    }
}
