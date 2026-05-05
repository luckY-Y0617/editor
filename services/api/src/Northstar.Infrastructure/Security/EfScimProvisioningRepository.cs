using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfScimProvisioningRepository : IScimProvisioningRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfScimProvisioningRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetUserByExternalForUpdateAsync(
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(
            user =>
                user.ExternalProvider == externalProvider &&
                user.ExternalSubjectId == externalSubjectId &&
                user.DeletedAt == null,
            cancellationToken);
    }

    public Task<User?> GetUserByEmailForUpdateAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Users.FirstOrDefaultAsync(
            user =>
                user.Email != null &&
                user.Email.ToLower() == email.ToLower() &&
                user.DeletedAt == null,
            cancellationToken);
    }

    public Task<ScimProvisionedUser?> GetUserForUpdateAsync(
        Guid workspaceId,
        Guid userId,
        string externalProvider,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in _dbContext.Users
            join member in _dbContext.WorkspaceMembers on user.Id equals member.UserId
            where member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                user.Id == userId &&
                user.DeletedAt == null &&
                user.ExternalProvider == externalProvider
            select new ScimProvisionedUser(user, member);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ScimProvisionedUser?> GetUserByExternalInWorkspaceForUpdateAsync(
        Guid workspaceId,
        string externalProvider,
        string externalSubjectId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in _dbContext.Users
            join member in _dbContext.WorkspaceMembers on user.Id equals member.UserId
            where member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                user.DeletedAt == null &&
                user.ExternalProvider == externalProvider &&
                user.ExternalSubjectId == externalSubjectId
            select new ScimProvisionedUser(user, member);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public Task<ScimProvisionedUser?> GetUserByEmailInWorkspaceForUpdateAsync(
        Guid workspaceId,
        string email,
        string externalProvider,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in _dbContext.Users
            join member in _dbContext.WorkspaceMembers on user.Id equals member.UserId
            where member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                user.DeletedAt == null &&
                user.ExternalProvider == externalProvider &&
                user.Email != null &&
                user.Email.ToLower() == email.ToLower()
            select new ScimProvisionedUser(user, member);

        return query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScimProvisionedUser>> GetUsersAsync(
        Guid workspaceId,
        string externalProvider,
        string? userName,
        string? externalSubjectId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in _dbContext.Users.AsNoTracking()
            join member in _dbContext.WorkspaceMembers.AsNoTracking() on user.Id equals member.UserId
            where member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                user.DeletedAt == null &&
                user.ExternalProvider == externalProvider &&
                (userName == null || (user.Email != null && user.Email.ToLower() == userName.ToLower())) &&
                (externalSubjectId == null || user.ExternalSubjectId == externalSubjectId)
            select new { User = user, Member = member };

        return await query
            .OrderBy(row => row.User.Email)
            .ThenBy(row => row.User.Id)
            .Skip(skip)
            .Take(take)
            .Select(row => new ScimProvisionedUser(row.User, row.Member))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountUsersAsync(
        Guid workspaceId,
        string externalProvider,
        string? userName,
        string? externalSubjectId,
        CancellationToken cancellationToken = default)
    {
        var query =
            from user in _dbContext.Users.AsNoTracking()
            join member in _dbContext.WorkspaceMembers.AsNoTracking() on user.Id equals member.UserId
            where member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                user.DeletedAt == null &&
                user.ExternalProvider == externalProvider &&
                (userName == null || (user.Email != null && user.Email.ToLower() == userName.ToLower())) &&
                (externalSubjectId == null || user.ExternalSubjectId == externalSubjectId)
            select user.Id;

        return query
            .CountAsync(cancellationToken);
    }

    public Task<WorkspaceMember?> GetWorkspaceMemberForUpdateAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers.FirstOrDefaultAsync(
            member => member.WorkspaceId == workspaceId && member.UserId == userId,
            cancellationToken);
    }

    public Task<WorkspaceGroup?> GetGroupByExternalForUpdateAsync(
        Guid workspaceId,
        string externalProvider,
        string externalGroupId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups.FirstOrDefaultAsync(
            group =>
                group.WorkspaceId == workspaceId &&
                group.ExternalProvider == externalProvider &&
                group.ExternalGroupId == externalGroupId &&
                group.ArchivedAt == null,
            cancellationToken);
    }

    public Task<WorkspaceGroup?> GetGroupForUpdateAsync(
        Guid workspaceId,
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups.FirstOrDefaultAsync(
            group =>
                group.WorkspaceId == workspaceId &&
                group.Id == groupId &&
                group.ArchivedAt == null,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ScimProvisionedGroup>> GetGroupsAsync(
        Guid workspaceId,
        string externalProvider,
        string? displayName,
        string? externalGroupId,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyGroupFilters(GroupQuery(workspaceId, externalProvider, tracking: false), displayName, externalGroupId);
        var groups = await query
            .OrderBy(group => group.Name)
            .ThenBy(group => group.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return await AttachMembersAsync(groups, tracking: false, cancellationToken);
    }

    public Task<int> CountGroupsAsync(
        Guid workspaceId,
        string externalProvider,
        string? displayName,
        string? externalGroupId,
        CancellationToken cancellationToken = default)
    {
        return ApplyGroupFilters(GroupQuery(workspaceId, externalProvider, tracking: false), displayName, externalGroupId)
            .CountAsync(cancellationToken);
    }

    public Task<bool> ActiveGroupNameExistsAsync(
        Guid workspaceId,
        string name,
        Guid? exceptGroupId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceGroups
            .AsNoTracking()
            .AnyAsync(
                group =>
                    group.WorkspaceId == workspaceId &&
                    group.ArchivedAt == null &&
                    group.Name == name &&
                    (exceptGroupId == null || group.Id != exceptGroupId.Value),
                cancellationToken);
    }

    public async Task<IReadOnlyList<ScimProvisionedUser>> GetUsersByIdsAsync(
        Guid workspaceId,
        string externalProvider,
        IReadOnlySet<Guid> userIds,
        CancellationToken cancellationToken = default)
    {
        if (userIds.Count == 0)
        {
            return [];
        }

        var query =
            from user in _dbContext.Users.AsNoTracking()
            join member in _dbContext.WorkspaceMembers.AsNoTracking() on user.Id equals member.UserId
            where member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                user.DeletedAt == null &&
                user.ExternalProvider == externalProvider &&
                userIds.Contains(user.Id)
            select new { User = user, Member = member };

        return await query
            .Select(row => new ScimProvisionedUser(row.User, row.Member))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ScimProvisionedGroupMember>> GetGroupMembersForUpdateAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var members = await GroupMembersQuery([groupId], tracking: true)
            .ToListAsync(cancellationToken);
        return members.Select(member => member.Entry).ToArray();
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

    private IQueryable<WorkspaceGroup> GroupQuery(
        Guid workspaceId,
        string externalProvider,
        bool tracking)
    {
        var groups = tracking ? _dbContext.WorkspaceGroups : _dbContext.WorkspaceGroups.AsNoTracking();

        return groups.Where(group =>
            group.WorkspaceId == workspaceId &&
            group.ArchivedAt == null &&
            group.ExternalProvider == externalProvider);
    }

    private static IQueryable<WorkspaceGroup> ApplyGroupFilters(
        IQueryable<WorkspaceGroup> query,
        string? displayName,
        string? externalGroupId)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            query = query.Where(group => group.Name == displayName);
        }

        if (!string.IsNullOrWhiteSpace(externalGroupId))
        {
            query = query.Where(group => group.ExternalGroupId == externalGroupId);
        }

        return query;
    }

    private async Task<IReadOnlyList<ScimProvisionedGroup>> AttachMembersAsync(
        IReadOnlyList<WorkspaceGroup> groups,
        bool tracking,
        CancellationToken cancellationToken)
    {
        if (groups.Count == 0)
        {
            return [];
        }

        var groupIds = groups.Select(group => group.Id).ToArray();
        var members = await GroupMembersQuery(groupIds, tracking)
            .ToListAsync(cancellationToken);
        var membersByGroup = members
            .GroupBy(member => member.GroupId)
            .ToDictionary(group => group.Key, group => group.Select(member => member.Entry).ToArray());

        return groups
            .Select(group => new ScimProvisionedGroup(
                group,
                membersByGroup.GetValueOrDefault(group.Id, [])))
            .ToArray();
    }

    private IQueryable<GroupMemberProjection> GroupMembersQuery(
        IReadOnlyCollection<Guid> groupIds,
        bool tracking)
    {
        var members = tracking ? _dbContext.WorkspaceGroupMembers : _dbContext.WorkspaceGroupMembers.AsNoTracking();
        var users = tracking ? _dbContext.Users : _dbContext.Users.AsNoTracking();

        return from member in members
               join user in users on member.UserId equals user.Id
               where groupIds.Contains(member.GroupId) &&
                   member.RemovedAt == null &&
                   user.DeletedAt == null
               select new GroupMemberProjection(
                   member.GroupId,
                   new ScimProvisionedGroupMember(member, user));
    }

    private sealed record GroupMemberProjection(
        Guid GroupId,
        ScimProvisionedGroupMember Entry);

}
