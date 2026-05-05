using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Module.Identity.Domain.Shared.Authorization;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Identity.Application.Authorization;

public sealed class SystemPermissionDefinitionProvider : PermissionDefinitionProvider, ITransientDependency
{
    private const string ModuleCode = "system";

    private static readonly IReadOnlyDictionary<string, string> GroupDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["users"] = "用户管理",
            ["roles"] = "角色管理",
            ["tenants"] = "租户管理",
            ["auditlogs"] = "审计日志",
            ["teams"] = "团队管理",
            ["permission-center"] = "权限中心",
            ["hangfire"] = "任务调度"
        };

    private static readonly IReadOnlyDictionary<string, string> PermissionDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [SystemPermissions.Users.View] = "用户查看",
            [SystemPermissions.Users.Manage] = "用户管理",
            [SystemPermissions.Roles.View] = "角色查看",
            [SystemPermissions.Roles.Manage] = "角色管理",
            [SystemPermissions.Tenants.View] = "租户查看",
            [SystemPermissions.Tenants.Manage] = "租户管理",
            [SystemPermissions.AuditLogs.View] = "审计日志查看",
            [SystemPermissions.Teams.View] = "团队查看",
            [SystemPermissions.Teams.Manage] = "团队管理",
            [SystemPermissions.PermissionCenter.View] = "权限中心查看",
            [SystemPermissions.PermissionCenter.Manage] = "权限中心管理",
            [SystemPermissions.Hangfire.Dashboard] = "任务调度面板",
            [SystemPermissions.Hangfire.Manage] = "任务调度管理"
        };

    public override void Define(IPermissionDefinitionContext context)
    {
        context.AddModule(ModuleCode, "系统管理", "系统级权限定义");

        var permissionCodes = GetSystemPermissionCodes();
        foreach (var permissionCode in permissionCodes)
        {
            var groupCode = ResolveGroupCode(permissionCode);
            var groupDisplayName = ResolveGroupDisplayName(groupCode);
            var group = context.AddGroup(ModuleCode, groupCode, groupDisplayName, $"系统模块 - {groupDisplayName}");

            var displayName = ResolvePermissionDisplayName(permissionCode, groupDisplayName);
            group.AddPermission(permissionCode, displayName, $"允许{displayName}");
        }
    }

    private static IReadOnlyList<string> GetSystemPermissionCodes()
    {
        var codes = new List<string>();
        var root = typeof(SystemPermissions);

        foreach (var nested in root.GetNestedTypes(BindingFlags.Public | BindingFlags.Static))
        {
            foreach (var field in nested.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType != typeof(string))
                {
                    continue;
                }

                var value = field.GetValue(null) as string;
                if (!string.IsNullOrWhiteSpace(value))
                {
                    codes.Add(value);
                }
            }
        }

        return codes.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ResolveGroupCode(string permissionCode)
    {
        var parts = permissionCode.Split('.', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? parts[1] : "default";
    }

    private static string ResolveGroupDisplayName(string groupCode)
        => GroupDisplayNames.TryGetValue(groupCode, out var name)
            ? name
            : groupCode;

    private static string ResolvePermissionDisplayName(string permissionCode, string groupDisplayName)
    {
        if (PermissionDisplayNames.TryGetValue(permissionCode, out var name))
        {
            return name;
        }

        if (permissionCode.EndsWith(".dashboard.view", StringComparison.OrdinalIgnoreCase))
        {
            return $"{groupDisplayName}面板查看";
        }

        if (permissionCode.EndsWith(".dashboard.manage", StringComparison.OrdinalIgnoreCase))
        {
            return $"{groupDisplayName}面板管理";
        }

        if (permissionCode.EndsWith(".view", StringComparison.OrdinalIgnoreCase))
        {
            return $"{groupDisplayName}查看";
        }

        if (permissionCode.EndsWith(".manage", StringComparison.OrdinalIgnoreCase))
        {
            return $"{groupDisplayName}管理";
        }

        return permissionCode;
    }
}

