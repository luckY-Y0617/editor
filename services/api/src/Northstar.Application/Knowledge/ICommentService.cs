using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface ICommentService
{
    Task<CommentThreadsResponse> ListThreadsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<CommentThreadDto> CreateThreadAsync(
        Guid documentId,
        CreateCommentThreadRequest request,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<CommentThreadDto> AddMessageAsync(
        Guid documentId,
        Guid threadId,
        AddCommentMessageRequest request,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<CommentThreadDto> ResolveThreadAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<CommentThreadDto> ReopenThreadAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default,
        string? shareToken = null);
}
