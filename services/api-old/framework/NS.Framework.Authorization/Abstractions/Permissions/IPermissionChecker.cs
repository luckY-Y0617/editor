using System;
using System.Threading;
using System.Threading.Tasks;

namespace NS.Framework.Authorization.Abstractions.Permissions;

public interface IPermissionChecker
{
    /// <summary>
    /// 检查用户是否拥有指定权限。
    /// 仅做授权执行判定，不包含业务状态判断。
    /// </summary>
    Task<bool> CheckAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}