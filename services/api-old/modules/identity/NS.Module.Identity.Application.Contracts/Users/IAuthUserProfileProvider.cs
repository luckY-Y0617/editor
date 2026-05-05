using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Application.Contracts.Teams.Dtos;
using NS.Module.Identity.Application.Contracts.Users.Dtos;

namespace NS.Module.Identity.Application.Contracts.Users;

/// <summary>
/// 认证/登录用的用户视图与权限聚合（Facade）
/// - 收口 UserInfoStore + PermissionStore
/// - 让 Auth 用例不直接依赖缓存实现细节（大厂标准）
/// </summary>
public interface IAuthUserProfileProvider
{
    Task<AuthUserDto> GetUserAsync(Guid userId, CancellationToken ct = default);

    Task<List<string>> GetPermissionsAsync(Guid userId, CancellationToken ct = default);
    
    /// <summary>登录后预热身份与权限缓存（可选，但建议用）</summary>
    Task WarmupAsync(Guid userId, CancellationToken ct = default);

    /// <summary>登出/踢人/权限变更时清缓存</summary>
    Task InvalidateAsync(Guid userId, CancellationToken ct = default);

    Task<List<UserTeamDto>> GetUserTeamsAsync(Guid userId);

    /// <summary>
    /// 根据用户ID批量查询用户基本信息
    /// 用于在列表页面批量获取用户显示信息（如头像、用户名等）
    /// </summary>
    Task<List<UserLookupDto>> FindByIdsAsync(
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据用户名查询用户
    /// </summary>
    Task<UserLookupDto?> FindByUserNameAsync(
        string userName,
        CancellationToken cancellationToken = default);
}