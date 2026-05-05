using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Comments;

namespace Northstar.Application.Knowledge;

public interface ICommentRepository
{
    Task<IReadOnlyList<CommentThreadDto>> ListThreadDtosAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    Task<CommentThreadDto?> GetThreadDtoAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task<CommentThread?> GetThreadAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default);

    Task AddThreadAsync(
        CommentThread thread,
        CommentMessage firstMessage,
        CancellationToken cancellationToken = default);

    Task AddMessageAsync(
        CommentMessage message,
        CancellationToken cancellationToken = default);
}
