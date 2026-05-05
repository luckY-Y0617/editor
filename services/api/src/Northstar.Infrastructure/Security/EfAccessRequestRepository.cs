using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfAccessRequestRepository : IAccessRequestRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfAccessRequestRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<AccessRequest?> GetForUpdateAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccessRequests
            .FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);
    }

    public Task<AccessRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccessRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request => request.Id == requestId, cancellationToken);
    }

    public Task<AccessRequest?> GetPendingAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.AccessRequests
            .AsNoTracking()
            .FirstOrDefaultAsync(request =>
                request.WorkspaceId == workspaceId &&
                request.ResourceType == resourceType &&
                request.ResourceId == resourceId &&
                request.SubjectType == subjectType &&
                request.SubjectId == subjectId &&
                request.Status == AccessRequestStatus.Pending,
                cancellationToken);
    }

    public async Task<IReadOnlyList<AccessRequest>> GetByWorkspaceAsync(
        Guid workspaceId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AccessRequests
            .AsNoTracking()
            .Where(request => request.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(request => request.Status == status);
        }

        return await query
            .OrderByDescending(request => request.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AccessRequest>> GetByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.AccessRequests
            .AsNoTracking()
            .Where(request =>
                request.WorkspaceId == workspaceId &&
                request.ResourceType == resourceType &&
                request.ResourceId == resourceId);

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(request => request.Status == status);
        }

        return await query
            .OrderByDescending(request => request.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetWorkspaceManagerUserIdsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.WorkspaceMembers
            .AsNoTracking()
            .Where(member =>
                member.WorkspaceId == workspaceId &&
                member.Status == WorkspaceMemberStatus.Active &&
                (member.Role == PermissionRole.Owner || member.Role == PermissionRole.Admin))
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default)
    {
        return _dbContext.AccessRequests.AddAsync(request, cancellationToken).AsTask();
    }
}
