using System;
using NS.Module.Identity.Domain.Shared.Consts;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Check = Volo.Abp.Check;

namespace NS.Module.Identity.Domain.Identities;

/// <summary>
/// RefreshToken（会话凭证）
/// - 只存 tokenHash（不存明文）
/// - 支持 Rotation：ParentTokenId / ReplacedByTokenId + TokenFamilyId（同一“token family”）
/// - 支持会话/设备维度：SessionId / ClientId / DeviceId
/// - 支持踢人/禁用/退出：Revoke
/// </summary>
[SugarTable("id_refresh_token")]
[SugarIndex("IX_RefreshTokens_TokenHash", nameof(TokenHash), OrderByType.Asc)]
[SugarIndex("IX_RefreshTokens_User", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_RefreshTokens_Tenant", nameof(TenantId), OrderByType.Asc)]
[SugarIndex("IX_RefreshTokens_Family", nameof(TokenFamilyId), OrderByType.Asc)]
[SugarIndex("IX_RefreshTokens_ExpiresAt", nameof(ExpiresAt), OrderByType.Asc)]
[SugarIndex("IX_RefreshTokens_Session", nameof(SessionId), OrderByType.Asc)]
public class RefreshToken : AuditedAggregateRoot<Guid>, IMultiTenant, ISoftDelete, IHasConcurrencyStamp
{
    public Guid UserId { get; private set; }

    /// <summary>同一登录“token family”，用于检测重放后“一锅端”撤销。</summary>
    public Guid TokenFamilyId { get; private set; }

    /// <summary>Rotation：上一代 token（可空：表示 family 的根）。</summary>
    [SugarColumn(IsNullable = true)]
    public Guid? ParentTokenId { get; private set; }

    /// <summary>Rotation：被替换后的下一代 token id（可空）。</summary>
    [SugarColumn(IsNullable = true)]
    public Guid? ReplacedByTokenId { get; private set; }

    /// <summary>RefreshToken 的 hash（不存明文）。</summary>
    public string TokenHash { get; private set; } = string.Empty;

    /// <summary>语义上的签发时间（也可用 CreationTime，但这个字段更直观）。</summary>
    public DateTime IssuedAt { get; private set; }

    public DateTime ExpiresAt { get; private set; }

    /// <summary>被撤销的时间（不为空即失效）。</summary>
    [SugarColumn(IsNullable = true)]
    public DateTime? RevokedAt { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? RevokedReason { get; private set; }

    /// <summary>会话 id：同一设备/浏览器的一条会话链路（强烈建议）。</summary>
    [SugarColumn(IsNullable = true)]
    public string? SessionId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? ClientId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? DeviceId { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? Fingerprint { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? CreatedByIp { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? CreatedByUserAgent { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? RevokedByIp { get; private set; }

    [SugarColumn(IsNullable = true)]
    public string? RevokedByUserAgent { get; private set; }

    public bool IsDeleted { get; set; }

    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    public RefreshToken()
    {
    }

    public RefreshToken(
        Guid id,
        Guid userId,
        Guid tokenFamilyId,
        string tokenHash,
        DateTime issuedAt,
        DateTime expiresAt,
        Guid? parentTokenId = null) : base(id)
    {
        UserId = userId;
        TokenFamilyId = tokenFamilyId;
        ParentTokenId = parentTokenId;

        SetTokenHash(tokenHash);
        SetLifetime(issuedAt, expiresAt);
    }

    public void SetTokenHash(string tokenHash)
    {
        Check.NotNullOrWhiteSpace(tokenHash, nameof(tokenHash), RefreshTokenConsts.TokenHashMaxLength);
        TokenHash = tokenHash;
    }

    public void SetLifetime(DateTime issuedAt, DateTime expiresAt)
    {
        if (expiresAt <= issuedAt)
        {
            throw new BusinessException("RefreshToken:InvalidLifetime")
                .WithData("issuedAt", issuedAt)
                .WithData("expiresAt", expiresAt);
        }

        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    public void SetClientContext(
        string? sessionId,
        string? clientId,
        string? deviceId,
        string? fingerprint,
        string? createdByIp,
        string? createdByUserAgent)
    {
        if (!string.IsNullOrWhiteSpace(sessionId))
        {
            Check.Length(sessionId, nameof(sessionId), RefreshTokenConsts.SessionIdMaxLength);
            SessionId = sessionId;
        }
        else
        {
            SessionId = null;
        }

        if (!string.IsNullOrWhiteSpace(clientId))
        {
            Check.Length(clientId, nameof(clientId), RefreshTokenConsts.ClientIdMaxLength);
            ClientId = clientId;
        }
        else
        {
            ClientId = null;
        }

        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            Check.Length(deviceId, nameof(deviceId), RefreshTokenConsts.DeviceIdMaxLength);
            DeviceId = deviceId;
        }
        else
        {
            DeviceId = null;
        }

        if (!string.IsNullOrWhiteSpace(fingerprint))
        {
            Check.Length(fingerprint, nameof(fingerprint), RefreshTokenConsts.FingerprintMaxLength);
            Fingerprint = fingerprint;
        }
        else
        {
            Fingerprint = null;
        }

        if (!string.IsNullOrWhiteSpace(createdByIp))
        {
            Check.Length(createdByIp, nameof(createdByIp), RefreshTokenConsts.IpAddressMaxLength);
            CreatedByIp = createdByIp;
        }

        if (!string.IsNullOrWhiteSpace(createdByUserAgent))
        {
            Check.Length(createdByUserAgent, nameof(createdByUserAgent), RefreshTokenConsts.UserAgentMaxLength);
            CreatedByUserAgent = createdByUserAgent;
        }
    }

    public bool IsExpired(DateTime now) => now >= ExpiresAt;

    public bool IsRevoked => RevokedAt.HasValue;

    public bool IsActive(DateTime now) => !IsRevoked && !IsExpired(now);

    public void MarkReplacedBy(Guid newTokenId)
    {
        ReplacedByTokenId = newTokenId;
    }

    public void Revoke(DateTime revokedAt, string reason, string? revokedByIp = null, string? revokedByUserAgent = null)
    {
        if (RevokedAt.HasValue)
        {
            return; // 幂等：重复撤销不报错
        }

        if (!string.IsNullOrWhiteSpace(reason))
        {
            Check.Length(reason, nameof(reason), RefreshTokenConsts.RevokeReasonMaxLength);
            RevokedReason = reason;
        }

        RevokedAt = revokedAt;

        if (!string.IsNullOrWhiteSpace(revokedByIp))
        {
            Check.Length(revokedByIp, nameof(revokedByIp), RefreshTokenConsts.IpAddressMaxLength);
            RevokedByIp = revokedByIp;
        }

        if (!string.IsNullOrWhiteSpace(revokedByUserAgent))
        {
            Check.Length(revokedByUserAgent, nameof(revokedByUserAgent), RefreshTokenConsts.UserAgentMaxLength);
            RevokedByUserAgent = revokedByUserAgent;
        }
    }

    /// <summary>
    /// 重放检测：如果一个已经撤销/过期的 token 又被拿来刷新，通常意味着泄露。
    /// 你可以在应用层捕捉此情况后：撤销整个 TokenFamilyId。
    /// </summary>
    public bool IsReuseDetected(DateTime now) => !IsActive(now);
}
