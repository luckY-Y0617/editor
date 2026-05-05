using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfWorkspaceMembershipQueryService : IWorkspaceMembershipQueryService
{
    private readonly NorthstarDbContext _dbContext;

    public EfWorkspaceMembershipQueryService(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<string?> GetActiveRoleAsync(
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member => member.WorkspaceId == workspaceId &&
                member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active)
            .Select(member => member.Role)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
