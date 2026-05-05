using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Module.Identity.Application.Contracts.Cache;
using NS.Module.Identity.Domain.Roles.Repositories;
using NS.Module.Identity.Domain.Users;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Identity.Application;

public sealed class SystemPermissionStore : IPermissionGrantProvider, ITransientDependency
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IDistributedCache<SystemPermissionCacheItem, SystemPermissionCacheKey> _cache;
    private readonly ICurrentTenant _currentTenant;
    private readonly ILogger<SystemPermissionStore> _logger;

    public string Name => "Identity.RolePermission";

    public int Priority => 0;

    public SystemPermissionStore(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IDistributedCache<SystemPermissionCacheItem, SystemPermissionCacheKey> cache,
        ICurrentTenant currentTenant,
        ILogger<SystemPermissionStore> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _cache = cache;
        _currentTenant = currentTenant;
        _logger = logger;
    }

    public async Task<PermissionCheckResult> CheckAsync(PermissionCheckContext context, CancellationToken ct = default)
    {
        if (context.UserId == Guid.Empty)
        {
            return PermissionCheckResult.Undefined(Name);
        }

        if (string.IsNullOrWhiteSpace(context.PermissionCode))
        {
            return PermissionCheckResult.Undefined(Name);
        }

        var permissions = await GetSystemPermissionsAsync(context.UserId, ct);
        var normalized = context.PermissionCode.Trim();

        var granted = permissions.Contains("*") || permissions.Contains(normalized);

        _logger.LogDebug(
            "SystemPermissionStore.Check: User={UserId}, Tenant={TenantId}, Permission={Permission}, Granted={Granted}",
            context.UserId, _currentTenant.Id, normalized, granted);

        return granted
            ? PermissionCheckResult.Granted(Name, "User has permission via role assignment")
            : PermissionCheckResult.Undefined(Name);
    }

    /// <summary>
    /// 获取用户所有系统级权限（带缓存）
    /// </summary>
    public async Task<HashSet<string>> GetSystemPermissionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var tenantId = _currentTenant.Id;
        var cacheKey = new SystemPermissionCacheKey(userId, tenantId);

        // 1) 从缓存读取
        var cached = await _cache.GetAsync(cacheKey, token: cancellationToken);
        if (cached is { PermissionCodes: not null })
        {
            _logger.LogDebug(
                "SystemPermissionStore.CacheHit: User={UserId}, Tenant={TenantId}, Count={Count}",
                userId, tenantId, cached.PermissionCodes.Count);

            return cached.PermissionCodes;
        }

        // 2) 缓存未命中，从数据库加载
        var cacheItem = await LoadPermissionsFromDatabaseAsync(userId, tenantId, cancellationToken);

        // 3) 写入缓存（TTL 建议配置化：先给合理默认 30min）
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30)
        };

        await _cache.SetAsync(cacheKey, cacheItem, options, token: cancellationToken);

        _logger.LogDebug(
            "SystemPermissionStore.CacheSet: User={UserId}, Tenant={TenantId}, Count={Count}",
            userId, tenantId, cacheItem.PermissionCodes.Count);

        return cacheItem.PermissionCodes;
    }

    /// <summary>
    /// 失效用户的系统权限缓存
    /// </summary>
    public async Task InvalidateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty) return;

        var tenantId = _currentTenant.Id;
        var cacheKey = new SystemPermissionCacheKey(userId, tenantId);

        await _cache.RemoveAsync(cacheKey, token: cancellationToken);

        _logger.LogDebug(
            "SystemPermissionStore.Invalidate: User={UserId}, Tenant={TenantId}",
            userId, tenantId);
    }

    /// <summary>
    /// 从数据库加载用户系统权限
    /// </summary>
    private async Task<SystemPermissionCacheItem> LoadPermissionsFromDatabaseAsync(
        Guid userId,
        Guid? tenantId,
        CancellationToken cancellationToken)
    {
        // 1) 用户 -> 角色
        var roleIds = await _userRepository.GetRoleIdsAsync(userId, cancellationToken);

        // 2) 角色 -> 权限码（目前是 N+1，后续你可以在 RoleRepo 上做批量优化）
        var permissionCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var roleId in roleIds)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var codes = await _roleRepository.GetPermissionCodesAsync(roleId, cancellationToken);

            foreach (var code in codes)
            {
                if (string.IsNullOrWhiteSpace(code)) continue;
                permissionCodes.Add(code.Trim());
            }
        }

        _logger.LogDebug(
            "SystemPermissionStore.LoadFromDb: User={UserId}, Tenant={TenantId}, RoleCount={RoleCount}, PermissionCount={Count}",
            userId, tenantId, roleIds.Count, permissionCodes.Count);

        return new SystemPermissionCacheItem
        {
            UserId = userId,
            TenantId = tenantId,
            PermissionCodes = permissionCodes,
            CachedAtUtc = DateTime.UtcNow
        };
    }
}
