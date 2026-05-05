using System.Collections.Generic;

namespace NS.Framework.Authorization.Abstractions.Permissions;

public sealed class PermissionDefinition
{
    public PermissionDefinition(
        string name,
        string groupName,
        string moduleCode,
        string? displayName = null,
        string? description = null,
        int order = 0)
    {
        Name = name;
        GroupName = groupName;
        ModuleCode = moduleCode;
        DisplayName = displayName ?? name;
        Description = description;
        Order = order;
    }

    public string Name { get; }
    public string GroupName { get; }
    public string ModuleCode { get; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
}

public sealed class PermissionModuleDefinition
{
    public PermissionModuleDefinition(
        string code,
        string? displayName = null,
        string? description = null,
        int order = 0)
    {
        Code = code;
        DisplayName = displayName ?? code;
        Description = description;
        Order = order;
    }

    public string Code { get; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
}

public sealed class PermissionGroupDefinition
{
    public PermissionGroupDefinition(
        string name,
        string moduleCode,
        string? displayName = null,
        string? description = null,
        int order = 0)
    {
        Name = name;
        ModuleCode = moduleCode;
        DisplayName = displayName ?? name;
        Description = description;
        Order = order;
    }

    public string Name { get; }
    public string ModuleCode { get; }
    public string DisplayName { get; set; }
    public string? Description { get; set; }
    public int Order { get; set; }
    public List<PermissionDefinition> Permissions { get; } = new();

    public PermissionDefinition AddPermission(
        string name,
        string? displayName = null,
        string? description = null,
        int order = 0)
    {
        var permission = new PermissionDefinition(
            name,
            Name,
            ModuleCode,
            displayName,
            description,
            order);
        Permissions.Add(permission);
        return permission;
    }
}

