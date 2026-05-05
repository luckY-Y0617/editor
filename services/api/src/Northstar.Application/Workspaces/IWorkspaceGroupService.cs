using Northstar.Contracts.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IWorkspaceGroupService
{
    Task<WorkspaceGroupsResponse> GetGroupsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<WorkspaceGroupDetailDto> GetGroupAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default);
    Task<WorkspaceGroupDto> CreateGroupAsync(Guid workspaceId, CreateWorkspaceGroupRequest request, CancellationToken cancellationToken = default);
    Task<WorkspaceGroupDto> UpdateGroupAsync(Guid workspaceId, Guid groupId, UpdateWorkspaceGroupRequest request, CancellationToken cancellationToken = default);
    Task ArchiveGroupAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default);
    Task<WorkspaceGroupMemberDto> AddMemberAsync(Guid workspaceId, Guid groupId, AddWorkspaceGroupMemberRequest request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid workspaceId, Guid groupId, Guid userId, CancellationToken cancellationToken = default);
}
