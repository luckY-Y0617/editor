using System;
using System.Collections.Generic;

namespace NS.Module.Identity.Domain.Shared.Events;

/// <summary>
/// 角色权限变更事件（领域事件）
/// 当角色的 PermissionCode 列表发生变化时触发，用于清除相关用户的权限缓存
/// </summary>
public class RolePermissionChangedEvent
{
    public Guid RoleId { get; set; }
    public List<string> PermissionCodes { get; set; } = new();
}

