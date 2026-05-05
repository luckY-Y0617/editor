using System.Threading;
using System.Threading.Tasks;

namespace NS.Framework.Authorization.Abstractions.Permissions;

/// <summary>
/// 权限授予提供者
/// - 负责判断用户是否被授予指定权限
/// - 支持多实现，由 Executor 组合决策
/// - 由 Identity 模块等实现
/// </summary>
public interface IPermissionGrantProvider
{
    /// <summary>
    /// Provider 名称（用于日志、调试、溯源）
    /// </summary>
    string Name { get; }

    /// <summary>
    /// 优先级（数值越小优先级越高，默认 0）
    /// </summary>
    int Priority => 0;

    /// <summary>
    /// 检查用户是否被授予指定权限
    /// </summary>
    /// <param name="context">检查上下文</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>
    /// - Granted: 明确授予
    /// - Prohibited: 明确禁止（短路）
    /// - Undefined: 不处理，继续下一个 Provider
    /// </returns>
    Task<PermissionCheckResult> CheckAsync(PermissionCheckContext context, CancellationToken cancellationToken = default);
}
