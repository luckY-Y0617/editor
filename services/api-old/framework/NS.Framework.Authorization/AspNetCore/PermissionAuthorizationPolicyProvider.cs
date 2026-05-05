using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace NS.Framework.Authorization.AspNetCore;

/// <summary>
/// 识别以下 policyName，并动态生成包含 PermissionRequirement 的 AuthorizationPolicy：
/// - perm:{code}                 （Single）
/// - perm:all:{code1,code2,...}  （AND）
/// - perm:any:{code1,code2,...}  （OR）
/// 其他 policyName 交给基类（DefaultAuthorizationPolicyProvider）处理。
/// </summary>
public sealed class PermissionAuthorizationPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new(StringComparer.OrdinalIgnoreCase);

    public PermissionAuthorizationPolicyProvider(
        IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
        {
            return base.GetPolicyAsync(policyName);
        }

        // 只接管 perm: / perm:all: / perm:any:
        if (!PermissionRequirement.TryParsePolicyName(policyName, out var mode, out var permissionCodes))
        {
            return base.GetPolicyAsync(policyName);
        }

        var normalizedPolicyName = policyName.Trim();

        // 缓存：同一个 policyName 不重复构建
        var policy = _cache.GetOrAdd(
            normalizedPolicyName,
            _ => BuildPermissionPolicy(mode, permissionCodes));

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    private AuthorizationPolicy BuildPermissionPolicy(
        PermissionRequirementMode mode,
        IReadOnlyList<string> permissionCodes)
    {
        var requirement = new PermissionRequirement(mode, permissionCodes);

        // 关键：权限型 policy 默认要求已登录（否则匿名用户也会进入 handler）
        return new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(requirement)
            .Build();
    }
}
