using Microsoft.EntityFrameworkCore;
using Northstar.Application.Security;
using Northstar.Domain.Security;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Security;

public sealed class EfScimTokenRepository : IScimTokenRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfScimTokenRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task AddAsync(ScimToken token, CancellationToken cancellationToken = default)
    {
        return _dbContext.ScimTokens.AddAsync(token, cancellationToken).AsTask();
    }

    public Task<ScimToken?> GetByTokenHashForUpdateAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ScimTokens
            .FirstOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<ScimToken>> GetByWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ScimTokens
            .AsNoTracking()
            .Where(token => token.WorkspaceId == workspaceId)
            .OrderByDescending(token => token.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public Task<ScimToken?> GetForUpdateAsync(
        Guid workspaceId,
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.ScimTokens
            .FirstOrDefaultAsync(
                token => token.WorkspaceId == workspaceId && token.Id == tokenId,
                cancellationToken);
    }
}
