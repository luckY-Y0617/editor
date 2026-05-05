namespace NS.Framework.Authorization.Abstractions.Permissions;

public interface IPermissionDefinitionContext
{
    PermissionModuleDefinition AddModule(
        string code,
        string? displayName = null,
        string? description = null,
        int order = 0);

    PermissionModuleDefinition? GetModuleOrNull(string code);

    void RemoveModule(string code);

    PermissionGroupDefinition AddGroup(
        string moduleCode,
        string name,
        string? displayName = null,
        string? description = null,
        int order = 0);

    PermissionGroupDefinition? GetGroupOrNull(string moduleCode, string name);

    void RemoveGroup(string moduleCode, string name);
}

