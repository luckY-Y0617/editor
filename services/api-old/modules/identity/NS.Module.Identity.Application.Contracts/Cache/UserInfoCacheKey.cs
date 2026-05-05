using System;

namespace NS.Module.Identity.Application.Contracts.Cache;

/// <summary>
/// 用户信息缓存 Key
/// - UserId + TenantId
/// - 统一前缀：Identity:UserInfo
/// </summary>
public readonly record struct UserInfoCacheKey(Guid UserId, Guid? TenantId)
{
    public override string ToString()
        => TenantId.HasValue
            ? $"Identity:UserInfo:{TenantId}:{UserId}"
            : $"Identity:UserInfo:Host:{UserId}";
}