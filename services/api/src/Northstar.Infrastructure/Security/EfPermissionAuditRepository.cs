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

    public async Task<PermissionAuditPage> SearchWorkspaceAsync(
        PermissionAuditQuery query,
        CancellationToken cancellationToken = default)
    {
        var auditQuery = _dbContext.PermissionAuditEvents
            .AsNoTracking()
            .Where(audit => audit.WorkspaceId == query.WorkspaceId);

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditQuery = auditQuery.Where(audit => audit.Action == query.Action);
        }

        if (!string.IsNullOrWhiteSpace(query.ResourceType))
        {
            auditQuery = auditQuery.Where(audit => audit.ResourceType == query.ResourceType);
        }

        if (query.ResourceId.HasValue)
        {
            auditQuery = auditQuery.Where(audit => audit.ResourceId == query.ResourceId.Value);
        }

        if (query.ActorId.HasValue)
        {
            auditQuery = auditQuery.Where(audit => audit.ActorId == query.ActorId.Value);
        }

        if (query.From.HasValue)
        {
            auditQuery = auditQuery.Where(audit => audit.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            auditQuery = auditQuery.Where(audit => audit.CreatedAt <= query.To.Value);
        }

        var totalCount = await auditQuery.CountAsync(cancellationToken);
        var events = await auditQuery
            .OrderByDescending(audit => audit.CreatedAt)
            .ThenByDescending(audit => audit.Id)
            .Skip(query.Offset)
            .Take(query.Limit)
            .ToListAsync(cancellationToken);
        var actorIds = events
            .Select(audit => audit.ActorId)
            .Where(actorId => actorId.HasValue)
            .Select(actorId => actorId!.Value)
            .Distinct()
            .ToArray();
        var actors = actorIds.Length == 0
            ? new Dictionary<Guid, ActorProjection>()
            : await _dbContext.Users
                .AsNoTracking()
                .Where(user => actorIds.Contains(user.Id))
                .Select(user => new ActorProjection(user.Id, user.DisplayName, user.Email))
                .ToDictionaryAsync(user => user.Id, cancellationToken);

        return new PermissionAuditPage(
            events.Select(audit =>
            {
                actors.TryGetValue(audit.ActorId ?? Guid.Empty, out var actor);
                return new PermissionAuditRow(audit, actor?.Name, actor?.Email);
            }).ToArray(),
            totalCount);
    }

    private sealed record ActorProjection(Guid Id, string Name, string? Email);
}
