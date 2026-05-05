using Microsoft.EntityFrameworkCore;
using Northstar.Application.Workspaces;
using Northstar.Domain.Users;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Workspaces;

public sealed class EfWorkspaceMemberRepository : IWorkspaceMemberRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfWorkspaceMemberRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> WorkspaceExistsAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Workspaces
            .AsNoTracking()
            .AnyAsync(workspace => workspace.Id == workspaceId && workspace.DeletedAt == null, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkspaceMemberReadModel>> GetMembersAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from member in _dbContext.WorkspaceMembers.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            where member.WorkspaceId == workspaceId && user.DeletedAt == null
            orderby member.Role == WorkspaceMemberRole.Owner ? 0 :
                    member.Role == WorkspaceMemberRole.Admin ? 1 :
                    member.Role == WorkspaceMemberRole.Editor ? 2 : 3,
                user.DisplayName
            select new WorkspaceMemberReadModel(
                user.Id,
                user.Email,
                user.DisplayName,
                member.Role,
                member.Status,
                member.JoinedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Email != null && user.Email.ToLower() == email.ToLower() && user.DeletedAt == null, cancellationToken);
    }

    public Task<WorkspaceMember?> GetMemberAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers
            .FirstOrDefaultAsync(member => member.WorkspaceId == workspaceId && member.UserId == userId, cancellationToken);
    }

    public Task<int> CountOwnersAsync(Guid workspaceId, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers
            .CountAsync(member => member.WorkspaceId == workspaceId &&
                member.Role == WorkspaceMemberRole.Owner &&
                member.Status == WorkspaceMemberStatus.Active,
                cancellationToken);
    }

    public Task AddMemberAsync(WorkspaceMember member, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers.AddAsync(member, cancellationToken).AsTask();
    }

    public void RemoveMember(WorkspaceMember member)
    {
        _dbContext.WorkspaceMembers.Remove(member);
    }
}
