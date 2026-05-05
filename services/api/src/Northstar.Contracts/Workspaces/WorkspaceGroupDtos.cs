namespace Northstar.Contracts.Workspaces;

public sealed record WorkspaceGroupDto(
    string Id,
    string WorkspaceId,
    string Name,
    string? Description,
    string Type,
    bool IsArchived,
    string? ExternalProvider,
    string? ExternalGroupId,
    DateTimeOffset? ExternalSyncedAt,
    int MembersCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record WorkspaceGroupMemberDto(
    string Id,
    string UserId,
    string? Email,
    string DisplayName,
    string? AddedBy,
    DateTimeOffset AddedAt,
    DateTimeOffset? ExpiresAt);

public sealed record WorkspaceGroupDetailDto(
    WorkspaceGroupDto Group,
    IReadOnlyList<WorkspaceGroupMemberDto> Members);

public sealed record WorkspaceGroupsResponse(IReadOnlyList<WorkspaceGroupDto> Groups);

public sealed record CreateWorkspaceGroupRequest(
    string Name,
    string? Description,
    string? Type);

public sealed record UpdateWorkspaceGroupRequest(
    string Name,
    string? Description);

public sealed record AddWorkspaceGroupMemberRequest(
    string UserId,
    DateTimeOffset? ExpiresAt);
