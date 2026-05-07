using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfWorkspaceGroupRepository : IWorkspaceGroupRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfWorkspaceGroupRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Workspaces
            .AsNoTracking()
            .AnyAsync(workspace => workspace.Id == workspaceId, cancellationToken);
    }

    public Task<bool> UserIsWorkspaceMemberAsync(Guid workspaceId, Guid userId, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers
            .AsNoTracking()
            .AnyAsync(member =>
                member.WorkspaceId == workspaceId &&
                member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceGroupReadModel>> GetGroupsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        return await _dbContext.WorkspaceGroups
            .AsNoTracking()
            .Where(group => group.WorkspaceId == workspaceId)
            .OrderBy(group => group.Name)
            .Select(group => new WorkspaceGroupReadModel(
                group.Id,
                group.WorkspaceId,
                group.Name,
                group.Description,
                group.Type,
                group.ArchivedAt,
                group.ExternalProvider,
                group.ExternalGroupId,
                group.ExternalSyncedAt,
                group.CreatedBy,
                group.CreatedAt,
                group.UpdatedAt,
                _dbContext.WorkspaceGroupMembers.Count(member =>
                    member.GroupId == group.Id &&
                    member.RemovedAt == null &&
                    (member.ExpiresAt == null || member.ExpiresAt > now))))
            .ToListAsync(cancellationToken);
    }

    public async Task<WorkspaceGroupDetailReadModel?> GetGroupDetailAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var group = await GetGroupsAsync(workspaceId, cancellationToken);
        var readModel = group.SingleOrDefault(item => item.Id == groupId);
        if (readModel is null)
        {
            return null;
        }

        var members = await _dbContext.WorkspaceGroupMembers
            .AsNoTracking()
            .Where(member =>
                member.GroupId == groupId &&
                member.RemovedAt == null &&
                (member.ExpiresAt == null || member.ExpiresAt > DateTimeOffset.UtcNow))
            .Join(
                _dbContext.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new WorkspaceGroupMemberReadModel(
                    member.Id,
                    member.GroupId,
                    member.UserId,
                    user.Email,
                    user.DisplayName,
                    member.AddedBy,
                    member.AddedAt,
                    member.ExpiresAt,
                    member.RemovedAt))
            .OrderBy(member => member.DisplayName)
            .ToListAsync(cancellationToken);

        return new WorkspaceGroupDetailReadModel(readModel, members);
    }

    public Task<WorkspaceGroup?> GetGroupAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups
            .AsNoTracking()
            .FirstOrDefaultAsync(group =>
                group.WorkspaceId == workspaceId &&
                group.Id == groupId,
                cancellationToken);
    }

    public Task<WorkspaceGroup?> GetGroupForUpdateAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups
            .FirstOrDefaultAsync(group =>
                group.WorkspaceId == workspaceId &&
                group.Id == groupId,
                cancellationToken);
    }

    public Task<bool> ActiveGroupNameExistsAsync(
        Guid workspaceId,
        string name,
        Guid? exceptGroupId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups
            .AsNoTracking()
            .AnyAsync(group =>
                group.WorkspaceId == workspaceId &&
                group.ArchivedAt == null &&
                group.Name == name &&
                (exceptGroupId == null || group.Id != exceptGroupId.Value),
                cancellationToken);
    }

    public Task<WorkspaceGroupMember?> GetActiveMemberForUpdateAsync(
        Guid groupId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroupMembers
            .FirstOrDefaultAsync(member =>
                member.GroupId == groupId &&
                member.UserId == userId &&
                member.RemovedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetActiveGroupIdsForUserAsync(
        Guid workspaceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkspaceGroupMembers
            .AsNoTracking()
            .Where(member =>
                member.UserId == userId &&
                member.RemovedAt == null &&
                (member.ExpiresAt == null || member.ExpiresAt > now))
            .Join(
                _dbContext.WorkspaceGroups.AsNoTracking(),
                member => member.GroupId,
                group => group.Id,
                (member, group) => group)
            .Where(group =>
                group.WorkspaceId == workspaceId &&
                group.ArchivedAt == null)
            .Select(group => group.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetActiveGroupMemberUserIdsAsync(
        Guid workspaceId,
        Guid groupId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkspaceGroupMembers
            .AsNoTracking()
            .Where(member =>
                member.GroupId == groupId &&
                member.RemovedAt == null &&
                (member.ExpiresAt == null || member.ExpiresAt > now))
            .Join(
                _dbContext.WorkspaceGroups.AsNoTracking(),
                member => member.GroupId,
                group => group.Id,
                (member, group) => new { member, group })
            .Where(joined =>
                joined.group.WorkspaceId == workspaceId &&
                joined.group.ArchivedAt == null)
            .Join(
                _dbContext.WorkspaceMembers.AsNoTracking(),
                joined => new { joined.group.WorkspaceId, joined.member.UserId },
                workspaceMember => new { workspaceMember.WorkspaceId, workspaceMember.UserId },
                (joined, workspaceMember) => new { joined.member.UserId, workspaceMember })
            .Where(joined => joined.workspaceMember.Status == WorkspaceMemberStatus.Active)
            .Select(joined => joined.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task AddGroupAsync(WorkspaceGroup group, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups.AddAsync(group, cancellationToken).AsTask();
    }

    public Task AddMemberAsync(WorkspaceGroupMember member, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroupMembers.AddAsync(member, cancellationToken).AsTask();
    }

}
