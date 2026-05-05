using Microsoft.EntityFrameworkCore;
using Northstar.Application.Workspaces;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Workspaces;

public sealed class EfIamSyncRepository : IIamSyncRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfIamSyncRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Workspaces
            .AsNoTracking()
            .AnyAsync(workspace => workspace.Id == workspaceId && workspace.DeletedAt == null, cancellationToken);
    }

    public Task<User?> GetUserByExternalAsync(
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.ExternalProvider == externalProvider &&
                user.ExternalSubjectId == externalSubjectId &&
                user.DeletedAt == null,
                cancellationToken);
    }

    public Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .FirstOrDefaultAsync(user =>
                user.Email != null &&
                user.Email.ToLower() == email.ToLower() &&
                user.DeletedAt == null,
                cancellationToken);
    }

    public Task<WorkspaceMember?> GetWorkspaceMemberForUpdateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(member =>
                member.WorkspaceId == workspaceId &&
                member.UserId == userId,
                cancellationToken);
    }

    public Task<WorkspaceGroup?> GetGroupByExternalForUpdateAsync(
        Guid workspaceId,
        string externalProvider,
        string externalGroupId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups
            .FirstOrDefaultAsync(group =>
                group.WorkspaceId == workspaceId &&
                group.ExternalProvider == externalProvider &&
                group.ExternalGroupId == externalGroupId,
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

    public async Task<IReadOnlyList<WorkspaceGroupMember>> GetGroupMembersForUpdateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkspaceGroupMembers
            .Where(member =>
                member.GroupId == groupId &&
                member.RemovedAt == null)
            .ToListAsync(cancellationToken);
    }

    public Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.AddAsync(user, cancellationToken).AsTask();
    }

    public Task AddWorkspaceMemberAsync(WorkspaceMember member, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers.AddAsync(member, cancellationToken).AsTask();
    }

    public Task AddGroupAsync(WorkspaceGroup group, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups.AddAsync(group, cancellationToken).AsTask();
    }

    public Task AddGroupMemberAsync(WorkspaceGroupMember member, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroupMembers.AddAsync(member, cancellationToken).AsTask();
    }
}
