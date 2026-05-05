namespace Northstar.Contracts.Workspaces;

public sealed record IamSyncRequest(
    string Provider,
    IReadOnlyList<IamSyncUserRequest> Users,
    IReadOnlyList<IamSyncGroupRequest> Groups);

public sealed record IamSyncUserRequest(
    string ExternalSubjectId,
    string? Email,
    string DisplayName,
    string? WorkspaceRole = null,
    string? WorkspaceId = null);

public sealed record IamSyncGroupRequest(
    string ExternalGroupId,
    string Name,
    string? Description,
    IReadOnlyList<string> Members,
    string? WorkspaceId = null);

public sealed record IamSyncResponse(
    IamSyncCountsDto Counts,
    IReadOnlyList<IamSyncUserMappingDto> Users,
    IReadOnlyList<IamSyncGroupMappingDto> Groups);

public sealed record IamSyncCountsDto(
    int Created,
    int Updated,
    int Removed,
    int Skipped,
    int UsersCreated,
    int UsersUpdated,
    int WorkspaceMembersCreated,
    int GroupsCreated,
    int GroupsUpdated,
    int MembersAdded,
    int MembersRemoved);

public sealed record IamSyncUserMappingDto(
    string ExternalSubjectId,
    string UserId,
    bool Created,
    bool WorkspaceMemberCreated);

public sealed record IamSyncGroupMappingDto(
    string ExternalGroupId,
    string GroupId,
    bool Created,
    int MembersAdded,
    int MembersRemoved);
