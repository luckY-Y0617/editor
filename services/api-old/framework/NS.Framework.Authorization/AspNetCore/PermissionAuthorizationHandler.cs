using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NS.Framework.Authorization.Abstractions.Permissions;
using Volo.Abp.Users;

namespace NS.Framework.Authorization.AspNetCore;

/// <summary>
/// 处理 PermissionRequirement：
/// - 从 HttpContext.Items 读取请求级资源上下文（统一注入）
/// - 调用你的 PermissionChecker 作最终裁决
/// - 支持 Single / All(AND) / Any(OR)
/// </summary>
public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IPermissionChecker _permissionChecker;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<PermissionAuthorizationHandler> _logger;

    public PermissionAuthorizationHandler(
        IPermissionChecker permissionChecker,
        IHttpContextAccessor httpContextAccessor,
        ICurrentUser currentUser,
        ILogger<PermissionAuthorizationHandler> logger)
    {
        _permissionChecker = permissionChecker;
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var permissionsText = string.Join(",", requirement.PermissionCodes);

        // 1) 必须已认证（PolicyProvider 已 RequireAuthenticatedUser，这里再保险）
        if (context.User.Identity?.IsAuthenticated != true)
        {
            _logger.LogDebug(
                "Permission auth skipped: unauthenticated. Mode={Mode}, Permissions={Permissions}",
                requirement.Mode, permissionsText);
            return;
        }

        // 2) 解析 UserId（优先用 ABP CurrentUser，避免 Claim 形态差异）
        var userId = TryGetUserId(context.User);
        if (userId is null || userId == Guid.Empty)
        {
            _logger.LogWarning(
                "Permission auth failed: cannot resolve userId. Mode={Mode}, Permissions={Permissions}",
                requirement.Mode, permissionsText);
            return;
        }

        // 3) 调用 PermissionChecker 裁决（按 Mode）
        var http = _httpContextAccessor.HttpContext;
        var cancellationToken = http?.RequestAborted ?? CancellationToken.None;
        bool granted;
        try
        {
            granted = requirement.Mode switch
            {
                PermissionRequirementMode.Single =>
                    await CheckSingleAsync(userId.Value, requirement.PermissionCodes[0], cancellationToken),

                PermissionRequirementMode.All =>
                    await CheckAllAsync(userId.Value, requirement.PermissionCodes, cancellationToken),

                PermissionRequirementMode.Any =>
                    await CheckAnyAsync(userId.Value, requirement.PermissionCodes, cancellationToken),

                _ => false
            };
        }
        catch (Exception ex)
        {
            // 异常按“拒绝”处理（fail-close）
            _logger.LogError(
                ex,
                "Permission auth error (fail-close). User={UserId}, Mode={Mode}, Permissions={Permissions}",
                userId, requirement.Mode, permissionsText);
            return;
        }

        if (granted)
        {
            context.Succeed(requirement);
            _logger.LogDebug(
                "Permission auth granted. User={UserId}, Mode={Mode}, Permissions={Permissions}",
                userId, requirement.Mode, permissionsText);
        }
        else
        {
            _logger.LogDebug(
                "Permission auth denied. User={UserId}, Mode={Mode}, Permissions={Permissions}",
                userId, requirement.Mode, permissionsText);
        }
    }

    private Task<bool> CheckSingleAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken)
        => _permissionChecker.CheckAsync(userId, permissionCode, cancellationToken);

    private async Task<bool> CheckAllAsync(
        Guid userId,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        // AND：任何一个失败则失败
        foreach (var code in permissionCodes)
        {
            if (!await _permissionChecker.CheckAsync(userId, code, cancellationToken))
            {
                return false;
            }
        }
        return true;
    }

    private async Task<bool> CheckAnyAsync(
        Guid userId,
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        // OR：任意一个成功则成功
        foreach (var code in permissionCodes)
        {
            if (await _permissionChecker.CheckAsync(userId, code, cancellationToken))
            {
                return true;
            }
        }
        return false;
    }

    private Guid? TryGetUserId(ClaimsPrincipal principal)
    {
        // ABP CurrentUser 优先
        if (_currentUser.IsAuthenticated && _currentUser.Id.HasValue)
        {
            return _currentUser.Id.Value;
        }

        // 兼容非 ABP/不同 ClaimType
        var id =
            principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
            principal.FindFirstValue("sub") ??
            principal.FindFirstValue("user_id");

        return Guid.TryParse(id, out var guid) ? guid : null;
    }

    // 不再从请求中读取业务上下文；业务状态判断由模块自行处理
}
