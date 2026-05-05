using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Comments;

public sealed class CommentThread
{
    private CommentThread()
    {
        Status = CommentThreadStatus.Open;
        AnchorJson = "{}";
    }

    public CommentThread(
        Guid documentId,
        string anchorJson,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        DocumentId = documentId;
        Status = CommentThreadStatus.Open;
        AnchorJson = RequiredJson(anchorJson, nameof(anchorJson));
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }
    public Guid DocumentId { get; private set; }
    public string Status { get; private set; }
    public string AnchorJson { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ResolvedAt { get; private set; }

    public void Resolve()
    {
        Status = CommentThreadStatus.Resolved;
        ResolvedAt = DateTimeOffset.UtcNow;
        UpdatedAt = ResolvedAt.Value;
    }

    public void Reopen()
    {
        Status = CommentThreadStatus.Open;
        ResolvedAt = null;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    private static string RequiredJson(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value;
    }
}
