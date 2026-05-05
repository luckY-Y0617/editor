using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Workspaces;

public interface IIamSyncRepository
{
    Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<User?> GetUserByExternalAsync(
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default);
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<WorkspaceMember?> GetWorkspaceMemberForUpdateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<WorkspaceGroup?> GetGroupByExternalForUpdateAsync(
        Guid workspaceId,
        string externalProvider,
        string externalGroupId,
        CancellationToken cancellationToken = default);
    Task<bool> ActiveGroupNameExistsAsync(
        Guid workspaceId,
        string name,
        Guid? exceptGroupId = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceGroupMember>> GetGroupMembersForUpdateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task AddWorkspaceMemberAsync(WorkspaceMember member, CancellationToken cancellationToken = default);
    Task AddGroupAsync(WorkspaceGroup group, CancellationToken cancellationToken = default);
    Task AddGroupMemberAsync(WorkspaceGroupMember member, CancellationToken cancellationToken = default);
}
