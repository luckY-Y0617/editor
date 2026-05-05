namespace Northstar.Contracts.Security;

public sealed record AccessRequestDto(
    string Id,
    string WorkspaceId,
    string ResourceType,
    string ResourceId,
    string RequesterId,
    string SubjectType,
    string SubjectId,
    string RequestedRole,
    string? Reason,
    string Status,
    string? DecidedBy,
    DateTimeOffset? DecidedAt,
    string? DecisionReason,
    string? ResultingGrantId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record AccessRequestsResponse(IReadOnlyList<AccessRequestDto> Requests);

public sealed record CreateAccessRequestRequest(
    string ResourceType,
    string ResourceId,
    string RequestedRole,
    string? Reason,
    string? SubjectType = null,
    string? SubjectId = null);

public sealed record ReviewAccessRequestRequest(
    string Decision,
    string? RoleKey,
    string? Reason,
    DateTimeOffset? ExpiresAt = null);

public sealed record CancelAccessRequestRequest(string? Reason);
