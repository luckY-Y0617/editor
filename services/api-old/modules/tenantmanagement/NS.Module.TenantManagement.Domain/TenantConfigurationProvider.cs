using System;
using System.Threading.Tasks;
using NS.Module.TenantManagement.Domain.Shared.Consts;
using Microsoft.Extensions.Localization;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.MultiTenancy.Localization;

namespace NS.Module.TenantManagement.Domain;

[Dependency(ReplaceServices = true)]
public class TenantConfigurationProvider : ITenantConfigurationProvider, ITransientDependency
{
    private readonly ITenantResolver _tenantResolver;
    private readonly ITenantStore _tenantStore;
    private readonly ITenantResolveResultAccessor _tenantResolveResultAccessor;
    private readonly IStringLocalizer<AbpMultiTenancyResource> _localizer;

    public TenantConfigurationProvider(
        ITenantResolver tenantResolver,
        ITenantStore tenantStore,
        ITenantResolveResultAccessor tenantResolveResultAccessor,
        IStringLocalizer<AbpMultiTenancyResource> localizer)
    {
        _tenantResolver = tenantResolver;
        _tenantStore = tenantStore;
        _tenantResolveResultAccessor = tenantResolveResultAccessor;
        _localizer = localizer;
    }

    public virtual async Task<TenantConfiguration?> GetAsync(bool saveResolveResult = false)
    {
        var resolveResult = await _tenantResolver.ResolveTenantIdOrNameAsync();

        if (saveResolveResult)
        {
            _tenantResolveResultAccessor.Result = resolveResult;
        }

        if (string.IsNullOrWhiteSpace(resolveResult.TenantIdOrName))
        {
            // Host 上下文（无租户）
            return null;
        }

        TenantConfiguration? tenantConfig;

        if (Guid.TryParse(resolveResult.TenantIdOrName, out var tenantId))
        {
            tenantConfig = await _tenantStore.FindAsync(tenantId);
        }
        else
        {
            tenantConfig = await _tenantStore.FindAsync(resolveResult.TenantIdOrName);
        }

        if (tenantConfig == null)
        {
            throw new BusinessException(TenantErrorCodes.TenantNotFound)
                .WithData("Tenant", resolveResult.TenantIdOrName)
                .WithData("Message", _localizer["TenantNotFoundMessage"])
                .WithData("Details", _localizer["TenantNotFoundDetails", resolveResult.TenantIdOrName]);
        }

        if (!tenantConfig.IsActive)
        {
            throw new BusinessException(TenantErrorCodes.TenantNotActive)
                .WithData("Tenant", resolveResult.TenantIdOrName)
                .WithData("Message", _localizer["TenantNotActiveMessage"])
                .WithData("Details", _localizer["TenantNotActiveDetails", resolveResult.TenantIdOrName]);
        }

        return tenantConfig;
    }
}

public class TenantConfigurationWithDetails : TenantConfiguration
{
    public TenantConfigurationWithDetails(Guid id, string name) : base(id, name)
    {
    }
    
    public string? DbType { get; set; }
}
