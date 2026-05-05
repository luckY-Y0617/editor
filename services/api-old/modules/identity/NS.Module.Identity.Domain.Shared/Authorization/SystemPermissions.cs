using System.Collections.Generic;

namespace NS.Module.Identity.Domain.Shared.Authorization;

public static class SystemPermissions
{
    public static class Users
    {
        public const string View = "system.users.view";
        public const string Manage = "system.users.manage";
    }

    public static class Roles
    {
        public const string View = "system.roles.view";
        public const string Manage = "system.roles.manage";
    }

    public static class Tenants
    {
        public const string View = "system.tenants.view";
        public const string Manage = "system.tenants.manage";
    }

    public static class AuditLogs
    {
        public const string View = "system.auditlogs.view";
    }

    public static class Teams
    {
        public const string View = "system.teams.view";
        public const string Manage = "system.teams.manage";
    }

    public static class PermissionCenter
    {
        public const string View = "system.permission-center.view";
        public const string Manage = "system.permission-center.manage";
    }

    /// <summary>
    /// 任务调度（Hangfire Dashboard）
    /// </summary>
    public static class Hangfire
    {
        /// <summary>
        /// 查看任务调度面板
        /// </summary>
        public const string Dashboard = "system.hangfire.dashboard";
        
        /// <summary>
        /// 管理任务（触发、删除、重试等）
        /// </summary>
        public const string Manage = "system.hangfire.manage";
    }

    // === 汇总集合，方便数据种子用 ===

    /// <summary>
    /// Host 管理员默认拥有的最小可用系统级权限
    /// </summary>
    public static readonly IReadOnlyList<string> HostAdminDefaults =
        new[]
        {
            Users.View, Users.Manage,
            Roles.View, Roles.Manage,
            PermissionCenter.View, PermissionCenter.Manage,
            Hangfire.Dashboard, Hangfire.Manage
        };

    /// <summary>
    /// 平台管理员默认拥有的系统级权限
    /// </summary>
    public static readonly IReadOnlyList<string> PlatformAdminDefaults = HostAdminDefaults;

    /// <summary>
    /// 租户管理员默认拥有的系统级权限（租户范围内）
    /// </summary>
    public static readonly IReadOnlyList<string> TenantAdminDefaults =
        new[]
        {
            Users.View, Users.Manage,
            Roles.View, Roles.Manage,
            AuditLogs.View
            // 一般不包含 Tenants.Manage（那是平台级 / 特殊入口才用）
        };

    /// <summary>
    /// 租户普通用户默认拥有的系统级权限
    /// </summary>
    public static readonly IReadOnlyList<string> TenantUserDefaults =
        new[]
        {
            Users.View
            // 普通用户一般不需要 Manage 级别的系统权限
        };
}

