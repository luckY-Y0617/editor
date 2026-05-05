using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfEmailInviteRepository : IEmailInviteRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfEmailInviteRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ResourceEmailInvite>> GetByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ResourceEmailInvites
            .AsNoTracking()
            .Where(invite =>
                invite.WorkspaceId == workspaceId &&
                invite.ResourceType == resourceType &&
                invite.ResourceId == resourceId)
            .OrderByDescending(invite => invite.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ResourceEmailInvite?> GetByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceEmailInvites
            .AsNoTracking()
            .FirstOrDefaultAsync(invite => invite.TokenHash == tokenHash, cancellationToken);
    }

    public Task<ResourceEmailInvite?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceEmailInvites
            .FirstOrDefaultAsync(invite => invite.TokenHash == tokenHash, cancellationToken);
    }

    public Task<ResourceEmailInvite?> GetForUpdateAsync(
        Guid inviteId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceEmailInvites
            .FirstOrDefaultAsync(invite => invite.Id == inviteId, cancellationToken);
    }

    public Task<ResourceEmailInvite?> GetPendingForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceEmailInvites
            .FirstOrDefaultAsync(invite =>
                invite.WorkspaceId == workspaceId &&
                invite.ResourceType == resourceType &&
                invite.ResourceId == resourceId &&
                invite.Email == email &&
                invite.Status == EmailInviteStatuses.Pending,
                cancellationToken);
    }

    public Task<string?> GetAcceptedRoleForEmailAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string email,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceEmailInvites
            .AsNoTracking()
            .Where(invite =>
                invite.WorkspaceId == workspaceId &&
                invite.ResourceType == resourceType &&
                invite.ResourceId == resourceId &&
                invite.Email == email &&
                invite.Status == EmailInviteStatuses.Accepted &&
                invite.RevokedAt == null &&
                invite.ExpiresAt > now)
            .OrderByDescending(invite => invite.AcceptedAt)
            .Select(invite => invite.RoleKey)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task AddAsync(ResourceEmailInvite invite, CancellationToken cancellationToken = default)
    {
        return _dbContext.ResourceEmailInvites.AddAsync(invite, cancellationToken).AsTask();
    }
}
