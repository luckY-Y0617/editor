using Northstar.Contracts.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IWorkspaceMembersService
{
    Task<WorkspaceMembersResponse> GetMembersAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<WorkspaceMemberDto> AddMemberAsync(Guid workspaceId, AddWorkspaceMemberRequest request, CancellationToken cancellationToken = default);
    Task<WorkspaceMemberDto> UpdateMemberAsync(Guid workspaceId, Guid userId, UpdateWorkspaceMemberRequest request, CancellationToken cancellationToken = default);
    Task RemoveMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
}
