using System;
using System.Collections.Generic;
using NS.Module.Identity.Domain.Shared.Enums;

namespace NS.Module.Identity.Application.Contracts.Cache;

/// <summary>
/// 用户身份信息缓存项（只包含“稳定、可复用”的身份视图）
/// </summary>
public sealed class UserInfoCacheItem
{
    public Guid UserId { get; init; }

    public Guid? TenantId { get; init; }

    public string UserName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }

    public GenderEnum? Gender { get; init; } = GenderEnum.Unknown;

    /// <summary>
    /// 用户角色 Id 列表（系统级，用于快速判定/展示）
    /// </summary>
    public IReadOnlyList<Guid>? RoleIds { get; init; } = Array.Empty<Guid>();

    /// <summary>
    /// 缓存时间（UTC），用于调试与审计
    /// </summary>
    public DateTime CachedAtUtc { get; init; }
    
    public string AvatarUrl { get; init; } = string.Empty;
}