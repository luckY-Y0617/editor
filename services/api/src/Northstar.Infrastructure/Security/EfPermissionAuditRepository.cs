using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfPermissionAuditRepository : IPermissionAuditRepository
{
    private const int AuditLimit = 100;

    private readonly NorthstarDbContext _dbContext;

    public EfPermissionAuditRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(PermissionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        return _dbContext.PermissionAuditEvents.AddAsync(auditEvent, cancellationToken).AsTask();
    }

    public async Task<IReadOnlyList<PermissionAuditEvent>> GetAsync(
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.PermissionAuditEvents
            .AsNoTracking()
            .Where(audit => audit.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            query = query.Where(audit => audit.ResourceType == resourceType);
        }

        if (resourceId.HasValue)
        {
            query = query.Where(audit => audit.ResourceId == resourceId.Value);
        }

        return await query
            .OrderByDescending(audit => audit.CreatedAt)
            .Take(AuditLimit)
            .ToListAsync(cancellationToken);
    }
}
