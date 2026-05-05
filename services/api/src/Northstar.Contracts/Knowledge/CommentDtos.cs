using System.Text.Json;

namespace Northstar.Contracts.Knowledge;

public sealed record CommentAuthorDto(
    string Id,
    string Name);

public sealed record CommentMessageDto(
    string Id,
    string ThreadId,
    string Body,
    CommentAuthorDto Author,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? DeletedAt);

public sealed record CommentThreadDto(
    string Id,
    string DocumentId,
    string Status,
    string AnchorStatus,
    JsonElement Anchor,
    IReadOnlyList<CommentMessageDto> Messages,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? ResolvedAt);

public sealed record CreateCommentThreadRequest(
    JsonElement Anchor,
    string Body);

public sealed record AddCommentMessageRequest(
    string Body);

public sealed record CommentThreadsResponse(
    IReadOnlyList<CommentThreadDto> Threads);
