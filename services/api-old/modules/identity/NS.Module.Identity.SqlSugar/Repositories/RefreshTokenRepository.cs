using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Identities.Repositories;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Identity.SqlSugar.Repositories;

public class RefreshTokenRepository :
    SqlSugarRepository<IdentityDbContext, RefreshToken, Guid>,
    IRefreshTokenRepository,
    ITransientDependency
{
    public RefreshTokenRepository(ISqlSugarDbContextProvider<IdentityDbContext> dbContextProvider) : base(dbContextProvider)
    {
    }

    public async Task<RefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = await GetSugarQueryableAsync();

        return await query
            .Where(x => x.TokenHash == tokenHash)
            .FirstAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveListByUserAsync(
        Guid userId,
        Guid? tenantId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = await GetSugarQueryableAsync();

        query = query.Where(x => x.UserId == userId);

        query = tenantId.HasValue
            ? query.Where(x => x.TenantId == tenantId)
            : query.Where(x => x.TenantId == null);

        return await query
            .Where(x => x.RevokedAt == null)
            .Where(x => x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<RefreshToken>> GetActiveListBySessionAsync(
        Guid userId,
        Guid? tenantId,
        string sessionId,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var query = await GetSugarQueryableAsync();

        query = query.Where(x => x.UserId == userId)
                     .Where(x => x.SessionId == sessionId);

        query = tenantId.HasValue
            ? query.Where(x => x.TenantId == tenantId)
            : query.Where(x => x.TenantId == null);

        return await query
            .Where(x => x.RevokedAt == null)
            .Where(x => x.ExpiresAt > now)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> RevokeFamilyAsync(
        Guid tokenFamilyId,
        DateTime revokedAt,
        string reason,
        string? revokedByIp = null,
        string? revokedByUserAgent = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = await GetSugarQueryableAsync();
        var list = await query
            .Where(x => x.TokenFamilyId == tokenFamilyId)
            .ToListAsync(cancellationToken);

        var count = 0;
        foreach (var token in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            token.Revoke(revokedAt, reason, revokedByIp, revokedByUserAgent);

            // UpdateAsync 来自 ISqlSugarRepository 基类能力（与您当前 ExternalAuthRepository 风格一致）
            await UpdateAsync(token, autoSave: true, cancellationToken: cancellationToken);

            count++;
        }

        return count;
    }
    
    public async Task<bool> TryMarkRotatedAsync(
        Guid tokenId,
        string expectedConcurrencyStamp,
        Guid replacedByTokenId,
        DateTime now,
        string reason,
        string? revokedByIp,
        string? revokedByUserAgent,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var db = await GetDbContextAsync();
        var newStamp = Guid.NewGuid().ToString("N");

        var affected = await db.Client.Updateable<RefreshToken>()
            .SetColumns(x => x.RevokedAt == now)
            .SetColumns(x => x.RevokedReason == reason)
            .SetColumns(x => x.ReplacedByTokenId == replacedByTokenId)
            .SetColumns(x => x.RevokedByIp == revokedByIp)
            .SetColumns(x => x.RevokedByUserAgent == revokedByUserAgent)
            .SetColumns(x => x.ConcurrencyStamp == newStamp)
            .Where(x => x.Id == tokenId)
            .Where(x => x.ConcurrencyStamp == expectedConcurrencyStamp)
            .Where(x => x.RevokedAt == null)
            .Where(x => x.ExpiresAt > now) // 可选但建议加硬
            .ExecuteCommandAsync(cancellationToken);

        return affected == 1;
    }

}
