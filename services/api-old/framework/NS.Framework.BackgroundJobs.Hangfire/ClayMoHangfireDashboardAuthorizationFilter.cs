using System;
using System.Security.Claims;
using System.Threading.Tasks;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Framework.Core.Utilities.Network;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.Security.Claims;

namespace NS.Framework.BackgroundJobs.Hangfire;

/// <summary>
/// ClayMo Hangfire Dashboard 授权过滤器
/// 1. 认证：支持 Session (Cookie) + JWT Bearer 双模式
/// 2. 授权：可选权限码检查（通过 IPermissionChecker）
/// 3. 安全：可选 IP 白名单（生产环境强烈建议）
/// </summary>
public sealed class ClayMoHangfireDashboardAuthorizationFilter : IDashboardAsyncAuthorizationFilter
{
    public async Task<bool> AuthorizeAsync(DashboardContext context)
    {
        var http = context.GetHttpContext();
        var sp = http.RequestServices;

        if (sp == null)
            return false;

        var options = sp.GetService<IOptions<HangfireDashboardAuthOptions>>()?.Value
                      ?? new HangfireDashboardAuthOptions();

        // 1) IP 白名单检查
        if (options.AllowedCidrs is { Length: > 0 })
        {
            var remoteIp = http.Connection.RemoteIpAddress;
            if (remoteIp is null || !CidrHelper.IsInCidrs(remoteIp, options.AllowedCidrs))
                return false;
        }

        // 2) 执行认证
        var schemeName = options.AuthenticationScheme ?? "Smart";
        var auth = await http.AuthenticateAsync(schemeName);

        if (!auth.Succeeded || auth.Principal?.Identity?.IsAuthenticated != true)
        {
            auth = await http.AuthenticateAsync();
            if (!auth.Succeeded || auth.Principal?.Identity?.IsAuthenticated != true)
                return false;
        }

        http.User = auth.Principal;

        // 3) 如果不需要权限检查，认证通过即可
        if (string.IsNullOrWhiteSpace(options.RequiredPermissionCode))
            return true;

        // 4) 解析 UserId
        if (!TryResolveUserId(http.User, options.UserIdClaimTypes, out var userId))
            return false;

        // 5) 权限检查
        var permissionChecker = sp.GetService<IPermissionChecker>();
        if (permissionChecker == null)
            return true; // 没有注册权限检查器，只做认证

        return await permissionChecker.CheckAsync(
            userId,
            options.RequiredPermissionCode,
            cancellationToken: http.RequestAborted);
    }

    private static bool TryResolveUserId(ClaimsPrincipal user, string[] claimTypes, out Guid userId)
    {
        userId = default;

        var candidates = claimTypes.Length > 0 ? claimTypes : DefaultUserIdClaimTypes;

        foreach (var type in candidates)
        {
            var v = user.FindFirstValue(type);
            if (string.IsNullOrWhiteSpace(v)) continue;
            if (Guid.TryParse(v, out userId)) return true;
        }

        foreach (var c in user.Claims)
        {
            if (Guid.TryParse(c.Value, out userId)) return true;
        }

        return false;
    }

    private static readonly string[] DefaultUserIdClaimTypes =
    [
        "sub",
        AbpClaimTypes.UserId,
        ClaimTypes.NameIdentifier,
        "user_id",
        "userid",
        "uid"
    ];
}
