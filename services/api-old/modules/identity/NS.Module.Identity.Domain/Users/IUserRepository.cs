using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Identity.Domain.Users;

public interface IUserRepository : ISqlSugarRepository<User, Guid>
{
    /// <summary>
    /// 根据用户名 + 密码查找并验证用户（登录专用）
    /// 查不到或密码不匹配均抛 BusinessException
    /// </summary>
    Task<User> FindAndVerifyAsync(
        string userName,
        string password,
        CancellationToken cancellationToken = default);

    Task<bool> UserNameExistsAsync(
        string normalizedUserName,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);

    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? excludeUserId = null,
        CancellationToken cancellationToken = default);


    // ============================
    // 用户-角色关系查询
    // ============================

    /// <summary>
    /// 获取用户所属的角色 Id 列表（当前租户上下文）
    /// </summary>
    Task<List<Guid>> GetRoleIdsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断用户是否属于指定角色
    /// （用于权限安全校验，如禁止修改自己所属角色权限）
    /// </summary>
    Task<bool> IsUserInRoleAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    // ============================
    // 用户-角色关系变更
    // ============================

    /// <summary>
    /// 重置用户在指定租户下的角色集合
    /// （会先清空，再重新分配）
    /// </summary>
    Task ResetRolesAsync(
        Guid userId,
        Guid? tenantId,
        IReadOnlyCollection<Guid> roleIds,
        CancellationToken cancellationToken = default);
}