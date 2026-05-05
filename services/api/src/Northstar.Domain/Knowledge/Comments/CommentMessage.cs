using Northstar.Domain.Shared;

namespace Northstar.Domain.Knowledge.Comments;

public sealed class CommentMessage
{
    private CommentMessage()
    {
        Body = string.Empty;
    }

    public CommentMessage(
        Guid threadId,
        string body,
        Guid authorUserId,
        Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        ThreadId = threadId;
        Body = Required(body, nameof(body));
        AuthorUserId = authorUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid ThreadId { get; private set; }
    public string Body { get; private set; }
    public Guid AuthorUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    private static string Required(string value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException(DomainErrorCodes.ValidationError, $"{parameterName} is required.");
        }

        return value;
    }
}
