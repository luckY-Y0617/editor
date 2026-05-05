using System;
using System.Collections.Generic;

namespace NS.Module.Identity.Domain.Shared.Events;

/// <summary>
/// 用户角色变更事件（领域事件）
/// 当用户的角色分配变更时触发，用于清除该用户的权限缓存
/// </summary>
public class UserRoleChangedEvent
{
    public Guid UserId { get; set; }
    public List<Guid> RoleIds { get; set; } = new();
}

