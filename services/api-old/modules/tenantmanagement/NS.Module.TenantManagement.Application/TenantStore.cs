using NS.Module.TenantManagement.Domain;
using NS.Module.TenantManagement.Domain.Repositories;
using Microsoft.Extensions.Caching.Distributed;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Threading;
using Volo.Abp.Uow;
using TenantConfigurationCacheItem = NS.Module.TenantManagement.Application.Contracts.Cache.TenantConfigurationCacheItem;

namespace NS.Module.TenantManagement.Application;

[Dependency(ReplaceServices = true)]
public class TenantStore : ITenantStore, ITransientDependency
{
    private readonly ISqlSugarTenantRepository _tenantRepository;
    private readonly IDistributedCache<TenantConfigurationCacheItem> _cache;
    private readonly IUnitOfWorkManager _unitOfWorkManager;
    private readonly ICurrentTenant _currentTenant;

    private readonly DistributedCacheEntryOptions _cacheEntryOptions;

    private const string CacheKeyByIdPrefix = "ClayMo:Tenant:Id:";
    private const string CacheKeyByNamePrefix = "ClayMo:Tenant:Name:";

    public TenantStore(
        ISqlSugarTenantRepository tenantRepository,
        IDistributedCache<TenantConfigurationCacheItem> cache,
        IUnitOfWorkManager unitOfWorkManager,
        ICurrentTenant currentTenant)
    {
        _tenantRepository = tenantRepository;
        _cache = cache;
        _unitOfWorkManager = unitOfWorkManager;
        _currentTenant = currentTenant;

        _cacheEntryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };
    }

    #region ITenantStore 异步接口

    public virtual async Task<TenantConfiguration?> FindAsync(Guid id)
    {
        var cacheKey = CacheKeyByIdPrefix + id;
        var cacheItem = await GetOrAddCacheItemAsync(cacheKey, () => GetTenantByIdAsync(id));

        return ToConfiguration(cacheItem);
    }

    public virtual async Task<TenantConfiguration?> FindAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var normalized = TenantAggregateRoot.NormalizeName(name);
        var cacheKey = CacheKeyByNamePrefix + normalized;

        var cacheItem = await GetOrAddCacheItemAsync(
            cacheKey,
            () => GetTenantByNormalizedNameAsync(normalized));

        return ToConfiguration(cacheItem);
    }

    public virtual async Task<IReadOnlyList<TenantConfiguration>> GetListAsync(bool includeDetails = false)
    {
        using (_currentTenant.Change(null))
        {
            using var uow = _unitOfWorkManager.Begin(isTransactional: false);

            var tenants = await _tenantRepository.GetListAsync(x => true, includeDetails: true);

            var list = tenants
                .Select(ToCacheItem)
                .Select(ToConfiguration)
                .Cast<TenantConfiguration>()
                .ToList();

            await uow.CompleteAsync();

            return list;
        }
    }

    #endregion

    #region 内部：从数据库查询并构建 CacheItem

    private async Task<TenantConfigurationCacheItem?> GetTenantByIdAsync(Guid id)
    {
        using (_currentTenant.Change(null))
        {
            var tenant = await _tenantRepository.FindAsync(id, includeDetails: true);

            return tenant == null ? null : ToCacheItem(tenant);
        }
    }

    private async Task<TenantConfigurationCacheItem?> GetTenantByNormalizedNameAsync(string normalizedName)
    {
        using (_currentTenant.Change(null))
        {
            using var uow = _unitOfWorkManager.Begin(isTransactional: false);

            var tenant = await _tenantRepository.FindAsync(x => x.NormalizedName == normalizedName, includeDetails: true);

            await uow.CompleteAsync();

            return tenant == null ? null : ToCacheItem(tenant);
        }
    }

    private async Task<TenantConfigurationCacheItem?> GetOrAddCacheItemAsync(
        string cacheKey,
        Func<Task<TenantConfigurationCacheItem?>> factory)
    {
        var cacheItem = await _cache.GetAsync(cacheKey);
        if (cacheItem != null)
        {
            return cacheItem;
        }

        cacheItem = await factory();
        if (cacheItem != null)
        {
            await _cache.SetAsync(cacheKey, cacheItem, _cacheEntryOptions);
        }

        return cacheItem;
    }

    #endregion

    #region 私有转换方法

    private static TenantConfigurationCacheItem ToCacheItem(TenantAggregateRoot tenant)
    {
        var connectionStrings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (tenant.ConnectionStrings is { Count: > 0 })
        {
            foreach (var cs in tenant.ConnectionStrings)
            {
                connectionStrings[cs.Name] = cs.Value;
            }
        }

        return new TenantConfigurationCacheItem
        {
            Id = tenant.Id,
            Name = tenant.Name,
            NormalizedName = tenant.NormalizedName,
            DbType = tenant.DbType.ToString(),
            ConnectionStrings = connectionStrings
        };
    }

    private static TenantConfiguration? ToConfiguration(TenantConfigurationCacheItem? cacheItem)
    {
        if (cacheItem == null)
        {
            return null;
        }

        var configuration = new TenantConfigurationWithDetails(cacheItem.Id, cacheItem.Name)
        {
            DbType = cacheItem.DbType
        };

        if (!string.IsNullOrWhiteSpace(cacheItem.NormalizedName))
        {
            configuration.NormalizedName = cacheItem.NormalizedName;
        }

        if (cacheItem.ConnectionStrings.Count > 0)
        {
            foreach (var kvp in cacheItem.ConnectionStrings)
            {
                configuration.ConnectionStrings![kvp.Key] = kvp.Value;
            }
        }

        return configuration;
    }

    #endregion

    #region ITenantStore 同步接口包装

    public virtual TenantConfiguration? Find(Guid id)
    {
        return AsyncHelper.RunSync(() => FindAsync(id));
    }

    public virtual TenantConfiguration? Find(string name)
    {
        return AsyncHelper.RunSync(() => FindAsync(name));
    }

    public virtual IReadOnlyList<TenantConfiguration> GetList(bool includeDetails = false)
    {
        return AsyncHelper.RunSync(() => GetListAsync(includeDetails));
    }

    #endregion
}
