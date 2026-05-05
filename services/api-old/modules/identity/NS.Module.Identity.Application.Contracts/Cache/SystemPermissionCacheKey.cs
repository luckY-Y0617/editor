using System;

namespace NS.Module.Identity.Application.Contracts.Cache;

/// <summary>
/// 系统权限缓存 Key
/// - UserId + TenantId
/// - 统一前缀：Identity:SystemPermissions
/// </summary>
public readonly record struct SystemPermissionCacheKey(Guid UserId, Guid? TenantId)
{
    public override string ToString()
        => TenantId.HasValue
            ? $"Identity:SystemPermissions:{TenantId}:{UserId}"
            : $"Identity:SystemPermissions:Host:{UserId}";
}