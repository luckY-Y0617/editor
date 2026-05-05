using System;
using System.Collections.Generic;
using NS.Framework.Authorization.Abstractions.Permissions;

namespace NS.Framework.Authorization.Core.Permissions;

public sealed class PermissionDefinitionContext : IPermissionDefinitionContext
{
    private readonly Dictionary<string, PermissionModuleDefinition> _modules =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PermissionGroupDefinition> _groups =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyDictionary<string, PermissionModuleDefinition> Modules => _modules;
    public IReadOnlyDictionary<string, PermissionGroupDefinition> Groups => _groups;

    public PermissionModuleDefinition AddModule(
        string code,
        string? displayName = null,
        string? description = null,
        int order = 0)
    {
        if (!_modules.TryGetValue(code, out var module))
        {
            module = new PermissionModuleDefinition(code, displayName, description, order);
            _modules.Add(code, module);
            return module;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            module.DisplayName = displayName;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            module.Description = description;
        }

        if (module.Order != order)
        {
            module.Order = order;
        }

        return module;
    }

    public PermissionModuleDefinition? GetModuleOrNull(string code)
        => _modules.TryGetValue(code, out var module) ? module : null;

    public void RemoveModule(string code)
        => _modules.Remove(code);

    public PermissionGroupDefinition AddGroup(
        string moduleCode,
        string name,
        string? displayName = null,
        string? description = null,
        int order = 0)
    {
        if (!_modules.ContainsKey(moduleCode))
        {
            AddModule(moduleCode);
        }

        var key = BuildGroupKey(moduleCode, name);
        if (!_groups.TryGetValue(key, out var group))
        {
            group = new PermissionGroupDefinition(name, moduleCode, displayName, description, order);
            _groups.Add(key, group);
            return group;
        }

        if (!string.IsNullOrWhiteSpace(displayName))
        {
            group.DisplayName = displayName;
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            group.Description = description;
        }

        if (group.Order != order)
        {
            group.Order = order;
        }

        return group;
    }

    public PermissionGroupDefinition? GetGroupOrNull(string moduleCode, string name)
        => _groups.TryGetValue(BuildGroupKey(moduleCode, name), out var group) ? group : null;

    public void RemoveGroup(string moduleCode, string name)
        => _groups.Remove(BuildGroupKey(moduleCode, name));

    private static string BuildGroupKey(string moduleCode, string groupName)
        => $"{moduleCode}:{groupName}";
}

