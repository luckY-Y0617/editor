using System;
using NS.Module.Identity.Domain.Shared.Consts;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Check = Volo.Abp.Check;

namespace NS.Module.Identity.Domain.Identities;

[SugarTable("id_external_auths")]
[SugarIndex("IX_ExternalAuths_User", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_ExternalAuths_Provider", nameof(ProviderName), OrderByType.Asc)]
[SugarIndex("IX_ExternalAuths_Tenant", nameof(TenantId), OrderByType.Asc)]
public class ExternalAuth : AuditedAggregateRoot<Guid>, IMultiTenant, ISoftDelete
{
    public Guid UserId { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ProviderKey { get; private set; } = string.Empty;
    public string? ProviderDisplayName { get; private set; }
    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public DateTime? ExpiresAt { get; private set; }
    public bool IsDeleted { get; set; }
    public Guid? TenantId { get; set; }

    public ExternalAuth() { }

    public ExternalAuth(Guid id, Guid userId, string providerName, string providerKey) : base(id)
    {
        UserId = userId;
        SetProvider(providerName, providerKey);
    }

    public void SetProvider(string providerName, string providerKey)
    {
        Check.NotNullOrWhiteSpace(providerName, nameof(providerName), ExternalAuthConsts.ProviderNameMaxLength);
        Check.NotNullOrWhiteSpace(providerKey, nameof(providerKey), ExternalAuthConsts.ProviderKeyMaxLength);
        ProviderName = providerName;
        ProviderKey = providerKey;
    }

    public void SetProviderDisplayName(string? providerDisplayName)
    {
        if (string.IsNullOrWhiteSpace(providerDisplayName))
        {
            ProviderDisplayName = null;
            return;
        }

        Check.Length(providerDisplayName, nameof(providerDisplayName), ExternalAuthConsts.ProviderDisplayNameMaxLength);
        ProviderDisplayName = providerDisplayName;
    }

    public void SetTokens(string? accessToken, string? refreshToken, DateTime? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            AccessToken = null;
        }
        else
        {
            Check.Length(accessToken, nameof(accessToken), ExternalAuthConsts.TokenMaxLength);
            AccessToken = accessToken;
        }

        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            RefreshToken = null;
        }
        else
        {
            Check.Length(refreshToken, nameof(refreshToken), ExternalAuthConsts.TokenMaxLength);
            RefreshToken = refreshToken;
        }

        ExpiresAt = expiresAt;
    }
}


