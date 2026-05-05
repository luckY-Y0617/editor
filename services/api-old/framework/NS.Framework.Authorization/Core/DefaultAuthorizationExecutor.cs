using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NS.Framework.Authorization.Abstractions.Permissions;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.Authorization.Core;

/// <summary>
/// 默认授权执行器
/// - 组合权限定义与权限授予
/// - 实现标准授权决策流程（Deny-first，短路）
/// </summary>
public sealed class DefaultAuthorizationExecutor :
    IPermissionChecker,
    IAuthorizationExecutor,
    ITransientDependency
{
    private readonly IPermissionDefinitionManager _definitionManager;
    private readonly IEnumerable<IPermissionGrantProvider> _grantProviders;
    private readonly ILogger<DefaultAuthorizationExecutor> _logger;

    public DefaultAuthorizationExecutor(
        IPermissionDefinitionManager definitionManager,
        IEnumerable<IPermissionGrantProvider> grantProviders,
        ILogger<DefaultAuthorizationExecutor> logger)
    {
        _definitionManager = definitionManager;
        _grantProviders = grantProviders.OrderBy(p => p.Priority).ToList();
        _logger = logger;
    }

    public async Task<bool> CheckAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var result = await AuthorizeAsync(userId, permissionCode, cancellationToken);
        return result.IsGranted;
    }

    public async Task<AuthorizationResult> AuthorizeAsync(Guid userId, string permissionCode, CancellationToken ct = default)
    {
        // 1. 检查权限编码是否为空
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            return new AuthorizationResult(
                false,
                AuthorizationDenyReasons.PermissionCodeEmpty,
                nameof(DefaultAuthorizationExecutor));
        }

        // 2. 检查权限是否已定义（代码真源）
        if (_definitionManager.GetOrNull(permissionCode) == null)
        {
            _logger.LogWarning(
                "Permission not defined in code. User={UserId}, Permission={Permission}",
                userId, permissionCode);
            return new AuthorizationResult(
                false,
                AuthorizationDenyReasons.NotDefined,
                nameof(DefaultAuthorizationExecutor));
        }

        // 3. 检查用户是否被授予权限（遍历所有 GrantProvider）
        var context = new PermissionCheckContext
        {
            UserId = userId,
            PermissionCode = permissionCode
        };

        var grantResult = await CheckGrantAsync(context, ct);

        if (grantResult.Result == PermissionGrantResult.Prohibited)
        {
            _logger.LogWarning(
                "Permission explicitly prohibited. User={UserId}, Permission={Permission}, Provider={Provider}, Reason={Reason}",
                userId, permissionCode, grantResult.ProviderName, grantResult.Reason);
            return new AuthorizationResult(
                false,
                AuthorizationDenyReasons.NotGranted,
                grantResult.ProviderName ?? nameof(DefaultAuthorizationExecutor));
        }

        if (grantResult.Result == PermissionGrantResult.Granted)
        {
            _logger.LogDebug(
                "Permission granted. User={UserId}, Permission={Permission}, Provider={Provider}",
                userId, permissionCode, grantResult.ProviderName);
            return new AuthorizationResult(
                true,
                null,
                grantResult.ProviderName ?? nameof(DefaultAuthorizationExecutor));
        }

        // Undefined - 没有任何 Provider 授予权限
        _logger.LogWarning(
            "Permission not granted by any provider. User={UserId}, Permission={Permission}",
            userId, permissionCode);
        return new AuthorizationResult(
            false,
            AuthorizationDenyReasons.NotGranted,
            nameof(DefaultAuthorizationExecutor));
    }

    /// <summary>
    /// 遍历所有 GrantProvider 检查权限授予
    /// - Prohibited 优先（短路，Deny-first）
    /// - 任一返回 Granted 即为已授予
    /// - 全部返回 Undefined 则为未授予
    /// </summary>
    private async Task<PermissionCheckResult> CheckGrantAsync(
        PermissionCheckContext context,
        CancellationToken cancellationToken)
    {
        PermissionCheckResult? grantedResult = null;

        foreach (var provider in _grantProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var result = await provider.CheckAsync(context, cancellationToken);

                _logger.LogDebug(
                    "Grant check: Provider={Provider}, User={UserId}, Permission={Permission}, Result={Result}",
                    provider.Name, context.UserId, context.PermissionCode, result.Result);

                // Prohibited 短路 - Deny-first
                if (result.Result == PermissionGrantResult.Prohibited)
                {
                    return result;
                }

                // 记录第一个 Granted 结果
                if (result.Result == PermissionGrantResult.Granted && grantedResult == null)
                {
                    grantedResult = result;
                }

                // Undefined - 继续下一个 Provider
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Grant provider {Provider} threw exception for User={UserId}, Permission={Permission}",
                    provider.Name, context.UserId, context.PermissionCode);
                // 继续下一个 Provider
            }
        }

        // 返回 Granted 或 Undefined
        return grantedResult ?? PermissionCheckResult.Undefined(nameof(DefaultAuthorizationExecutor));
    }
}
