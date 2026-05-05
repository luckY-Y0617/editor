using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfResourcePermissionRepository : IResourcePermissionRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfResourcePermissionRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<ResourceAccessPolicy?> GetPolicyAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(policy =>
                policy.WorkspaceId == workspaceId &&
                policy.ResourceType == resourceType &&
                policy.ResourceId == resourceId,
                cancellationToken);
    }

    public Task<ResourceAccessPolicy?> GetPolicyForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessPolicies
            .FirstOrDefaultAsync(policy =>
                policy.WorkspaceId == workspaceId &&
                policy.ResourceType == resourceType &&
                policy.ResourceId == resourceId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceAccessPolicy>> GetPoliciesForResourcesAsync(
        Guid workspaceId,
        string resourceType,
        IReadOnlyCollection<Guid> resourceIds,
        CancellationToken cancellationToken = default)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<ResourceAccessPolicy>();
        }

        return await _dbContext.ResourceAccessPolicies
            .AsNoTracking()
            .Where(policy =>
                policy.WorkspaceId == workspaceId &&
                policy.ResourceType == resourceType &&
                resourceIds.Contains(policy.ResourceId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceAccessGrant>> GetUserGrantsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await GetGrantsQuery(workspaceId, resourceType, resourceId)
            .Where(grant => grant.SubjectType == SubjectTypes.User)
            .OrderBy(grant => grant.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceAccessGrant>> GetGrantsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.RevokedAt == null)
            .OrderBy(grant => grant.GrantedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ResourceAccessGrant?> GetActiveUserGrantAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return GetActiveSubjectGrantAsync(
            workspaceId,
            resourceType,
            resourceId,
            SubjectTypes.User,
            userId,
            now,
            cancellationToken);
    }

    public Task<ResourceAccessGrant?> GetActiveSubjectGrantAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == subjectType &&
                grant.SubjectId == subjectId &&
                grant.RevokedAt == null &&
                (grant.ExpiresAt == null || grant.ExpiresAt > now))
            .OrderByDescending(grant => grant.GrantedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        IReadOnlyCollection<Guid> groupIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (groupIds.Count == 0)
        {
            return Array.Empty<ResourceAccessGrant>();
        }

        return await _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == SubjectTypes.Group &&
                groupIds.Contains(grant.SubjectId) &&
                grant.RevokedAt == null &&
                (grant.ExpiresAt == null || grant.ExpiresAt > now))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsForResourcesAsync(
        Guid workspaceId,
        string resourceType,
        IReadOnlyCollection<Guid> resourceIds,
        IReadOnlyCollection<Guid> groupIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (resourceIds.Count == 0 || groupIds.Count == 0)
        {
            return Array.Empty<ResourceAccessGrant>();
        }

        return await _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                resourceIds.Contains(grant.ResourceId) &&
                grant.SubjectType == SubjectTypes.Group &&
                groupIds.Contains(grant.SubjectId) &&
                grant.RevokedAt == null &&
                (grant.ExpiresAt == null || grant.ExpiresAt > now))
            .ToListAsync(cancellationToken);
    }

    public Task<ResourceAccessGrant?> GetGrantForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessGrants
            .FirstOrDefaultAsync(grant =>
                grant.Id == grantId &&
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId,
                cancellationToken);
    }

    public Task<ResourceAccessGrant?> GetUserGrantForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return GetSubjectGrantForUpdateAsync(
            workspaceId,
            resourceType,
            resourceId,
            SubjectTypes.User,
            userId,
            cancellationToken);
    }

    public Task<ResourceAccessGrant?> GetSubjectGrantForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessGrants
            .FirstOrDefaultAsync(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.SubjectType == subjectType &&
                grant.SubjectId == subjectId &&
                grant.RevokedAt == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ResourceAccessGrant>> GetActiveUserGrantsForResourcesAsync(
        Guid workspaceId,
        string resourceType,
        IReadOnlyCollection<Guid> resourceIds,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (resourceIds.Count == 0)
        {
            return Array.Empty<ResourceAccessGrant>();
        }

        return await _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                resourceIds.Contains(grant.ResourceId) &&
                grant.SubjectType == SubjectTypes.User &&
                grant.SubjectId == userId &&
                grant.RevokedAt == null &&
                (grant.ExpiresAt == null || grant.ExpiresAt > now))
            .ToListAsync(cancellationToken);
    }

    public Task AddPolicyAsync(ResourceAccessPolicy policy, CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessPolicies.AddAsync(policy, cancellationToken).AsTask();
    }

    public Task AddGrantAsync(ResourceAccessGrant grant, CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceAccessGrants.AddAsync(grant, cancellationToken).AsTask();
    }

    private IQueryable<ResourceAccessGrant> GetGrantsQuery(
        Guid workspaceId,
        string resourceType,
        Guid resourceId)
    {
        return _dbContext.ResourceAccessGrants
            .AsNoTracking()
            .Where(grant =>
                grant.WorkspaceId == workspaceId &&
                grant.ResourceType == resourceType &&
                grant.ResourceId == resourceId &&
                grant.RevokedAt == null);
    }
}
