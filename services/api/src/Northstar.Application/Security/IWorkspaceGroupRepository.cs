using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IWorkspaceGroupRepository
{
    Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<bool> UserIsWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkspaceGroupReadModel>> GetGroupsAsync(Guid workspaceId, CancellationToken cancellationToken = default);
    Task<WorkspaceGroupDetailReadModel?> GetGroupDetailAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default);
    Task<WorkspaceGroup?> GetGroupAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default);
    Task<WorkspaceGroup?> GetGroupForUpdateAsync(Guid workspaceId, Guid groupId, CancellationToken cancellationToken = default);
    Task<bool> ActiveGroupNameExistsAsync(Guid workspaceId, string name, Guid? exceptGroupId = null, CancellationToken cancellationToken = default);
    Task<WorkspaceGroupMember?> GetActiveMemberForUpdateAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetActiveGroupIdsForUserAsync(Guid workspaceId, Guid userId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Guid>> GetActiveGroupMemberUserIdsAsync(Guid workspaceId, Guid groupId, DateTimeOffset now, CancellationToken cancellationToken = default);
    Task AddGroupAsync(WorkspaceGroup group, CancellationToken cancellationToken = default);
    Task AddMemberAsync(WorkspaceGroupMember member, CancellationToken cancellationToken = default);
}

public sealed record WorkspaceGroupReadModel(
    Guid Id,
    Guid WorkspaceId,
    string Name,
    string? Description,
    string Type,
    DateTimeOffset? ArchivedAt,
    string? ExternalProvider,
    string? ExternalGroupId,
    DateTimeOffset? ExternalSyncedAt,
    Guid? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int MembersCount);

public sealed record WorkspaceGroupDetailReadModel(
    WorkspaceGroupReadModel Group,
    IReadOnlyList<WorkspaceGroupMemberReadModel> Members);

public sealed record WorkspaceGroupMemberReadModel(
    Guid Id,
    Guid GroupId,
    Guid UserId,
    string? Email,
    string DisplayName,
    Guid? AddedBy,
    DateTimeOffset AddedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RemovedAt);
