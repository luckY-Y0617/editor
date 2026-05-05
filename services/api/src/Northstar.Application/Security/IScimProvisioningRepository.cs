using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;

namespace Northstar.Application.Security;

public interface IScimProvisioningRepository
{
    Task<User?> GetUserByExternalForUpdateAsync(
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByEmailForUpdateAsync(
        string email,
        CancellationToken cancellationToken = default);

    Task<ScimProvisionedUser?> GetUserForUpdateAsync(
        Guid workspaceId,
        Guid userId,
        string externalProvider,
        CancellationToken cancellationToken = default);

    Task<ScimProvisionedUser?> GetUserByExternalInWorkspaceForUpdateAsync(
        Guid workspaceId,
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default);

    Task<ScimProvisionedUser?> GetUserByEmailInWorkspaceForUpdateAsync(
        Guid workspaceId,
        string email,
        string externalProvider,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScimProvisionedUser>> GetUsersAsync(
        Guid workspaceId,
        string externalProvider,
        string? userName,
        string? externalSubjectId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountUsersAsync(
        Guid workspaceId,
        string externalProvider,
        string? userName,
        string? externalSubjectId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceMember?> GetWorkspaceMemberForUpdateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceGroup?> GetGroupByExternalForUpdateAsync(
        Guid workspaceId,
        string externalProvider,
        string externalGroupId,
        CancellationToken cancellationToken = default);

    Task<WorkspaceGroup?> GetGroupForUpdateAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScimProvisionedGroup>> GetGroupsAsync(
        Guid workspaceId,
        string externalProvider,
        string? displayName,
        string? externalGroupId,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountGroupsAsync(
        Guid workspaceId,
        string externalProvider,
        string? displayName,
        string? externalGroupId,
        CancellationToken cancellationToken = default);

    Task<bool> ActiveGroupNameExistsAsync(
        Guid workspaceId,
        string name,
        Guid? exceptGroupId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScimProvisionedUser>> GetUsersByIdsAsync(
        Guid workspaceId,
        string externalProvider,
        IReadOnlySet<Guid> userIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScimProvisionedGroupMember>> GetGroupMembersForUpdateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default);

    Task AddUserAsync(User user, CancellationToken cancellationToken = default);

    Task AddWorkspaceMemberAsync(WorkspaceMember member, CancellationToken cancellationToken = default);

    Task AddGroupAsync(WorkspaceGroup group, CancellationToken cancellationToken = default);

    Task AddGroupMemberAsync(WorkspaceGroupMember member, CancellationToken cancellationToken = default);
}

public sealed record ScimProvisionedUser(User User, WorkspaceMember WorkspaceMember);

public sealed record ScimProvisionedGroup(
    WorkspaceGroup Group,
    IReadOnlyList<ScimProvisionedGroupMember> Members);

public sealed record ScimProvisionedGroupMember(WorkspaceGroupMember Member, User User);
