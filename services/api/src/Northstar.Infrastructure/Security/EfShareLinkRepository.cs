using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfShareLinkRepository : IShareLinkRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfShareLinkRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ShareLink>> GetActiveByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ShareLinks
            .AsNoTracking()
            .Where(link =>
                link.WorkspaceId == workspaceId &&
                link.ResourceType == resourceType &&
                link.ResourceId == resourceId &&
                link.RevokedAt == null &&
                (link.ExpiresAt == null || link.ExpiresAt > now))
            .OrderByDescending(link => link.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ShareLink?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ShareLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.TokenHash == tokenHash, cancellationToken);
    }

    public Task<ShareLink?> GetByIdAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ShareLinks
            .AsNoTracking()
            .FirstOrDefaultAsync(link => link.Id == shareLinkId, cancellationToken);
    }

    public Task<ShareLink?> GetForUpdateAsync(
        Guid shareLinkId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ShareLinks
            .FirstOrDefaultAsync(link => link.Id == shareLinkId, cancellationToken);
    }

    public async Task<IReadOnlyList<ShareLink>> SearchAsync(
        ShareLinkSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var links = _dbContext.ShareLinks
            .AsNoTracking()
            .Where(link => link.WorkspaceId == query.WorkspaceId);

        if (!string.IsNullOrWhiteSpace(query.ResourceType))
        {
            links = links.Where(link => link.ResourceType == query.ResourceType);
        }

        if (query.ResourceId.HasValue)
        {
            links = links.Where(link => link.ResourceId == query.ResourceId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Audience))
        {
            links = links.Where(link => link.Audience == query.Audience);
        }

        if (!string.IsNullOrWhiteSpace(query.RoleKey))
        {
            links = links.Where(link => link.RoleKey == query.RoleKey);
        }

        return await links
            .OrderByDescending(link => link.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task AddAsync(ShareLink link, CancellationToken cancellationToken = default)
    {
        return _dbContext.ShareLinks.AddAsync(link, cancellationToken).AsTask();
    }
}
