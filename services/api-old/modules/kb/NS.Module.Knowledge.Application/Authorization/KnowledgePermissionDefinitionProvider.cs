using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NS.Framework.Authorization.Abstractions.Permissions;
using NS.Module.Knowledge.Domain.Shared.Authorization;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.Application.Authorization;

public sealed class KnowledgePermissionDefinitionProvider : PermissionDefinitionProvider, ITransientDependency
{
    private const string ModuleCode = "kb";

    private static readonly IReadOnlyDictionary<string, string> GroupDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["base"] = "知识库",
            ["doc"] = "文档",
            ["tag"] = "标签",
            ["version"] = "版本",
            ["comment"] = "评论"
        };

    private static readonly IReadOnlyDictionary<string, string> ActionDisplayNames =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["view"] = "查看",
            ["create"] = "创建",
            ["manage"] = "管理",
            ["delete"] = "删除",
            ["edit"] = "编辑",
            ["move"] = "移动",
            ["restore"] = "恢复",
            ["like"] = "点赞"
        };

    public override void Define(IPermissionDefinitionContext context)
    {
        context.AddModule(ModuleCode, "知识库", "知识库相关权限定义");

        var permissionCodes = GetPermissionCodes(typeof(KnowledgePermissions));
        foreach (var permissionCode in permissionCodes)
        {
            var groupCode = ResolveGroupCode(permissionCode);
            var groupDisplayName = ResolveGroupDisplayName(groupCode);
            var group = context.AddGroup(ModuleCode, groupCode, groupDisplayName, $"知识库 - {groupDisplayName}");

            var displayName = ResolvePermissionDisplayName(permissionCode, groupDisplayName);
            group.AddPermission(permissionCode, displayName, $"允许{displayName}");
        }
    }

    private static IReadOnlyList<string> GetPermissionCodes(Type root)
    {
        var codes = new List<string>();
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
        return parts.Length >= 2 ? parts[1] : KnowledgePermissions.GroupName;
    }

    private static string ResolveGroupDisplayName(string groupCode)
        => GroupDisplayNames.TryGetValue(groupCode, out var name)
            ? name
            : groupCode;

    private static string ResolvePermissionDisplayName(string permissionCode, string groupDisplayName)
    {
        var action = permissionCode.Split('.', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (action != null && ActionDisplayNames.TryGetValue(action, out var name))
        {
            return $"{groupDisplayName}{name}";
        }

        return permissionCode;
    }
}

