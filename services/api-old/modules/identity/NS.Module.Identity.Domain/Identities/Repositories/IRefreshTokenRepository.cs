using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Identity.Domain.Identities.Repositories;

public interface IRefreshTokenRepository : ISqlSugarRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> GetActiveListByUserAsync(
        Guid userId,
        Guid? tenantId,
        DateTime now,
        CancellationToken cancellationToken = default);

    Task<List<RefreshToken>> GetActiveListBySessionAsync(
        Guid userId,
        Guid? tenantId,
        string sessionId,
        DateTime now,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 一锅端撤销同一 token family（重放/泄露场景最常用）。
    /// 注意：这里用“加载后逐个 Update”保证兼容你当前 Repository 基类能力；
    /// 如果你后续愿意用 SqlSugar Updateable 批量更新，可再优化成单条 SQL。
    /// </summary>
    Task<int> RevokeFamilyAsync(
        Guid tokenFamilyId,
        DateTime revokedAt,
        string reason,
        string? revokedByIp = null,
        string? revokedByUserAgent = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// CAS 原子轮换：只有在“未撤销 + 未过期 + 并发戳一致”的情况下，才把旧 token 标记为 Rotated 并写入 replacedBy。
    /// 返回 true 表示你拿到了“唯一成功权”，可以继续插入新 token；false 表示并发输了/已被用过。
    /// </summary>
    Task<bool> TryMarkRotatedAsync(
        Guid tokenId,
        string expectedConcurrencyStamp,
        Guid replacedByTokenId,
        DateTime now,
        string reason,
        string? revokedByIp,
        string? revokedByUserAgent,
        CancellationToken cancellationToken = default);
}