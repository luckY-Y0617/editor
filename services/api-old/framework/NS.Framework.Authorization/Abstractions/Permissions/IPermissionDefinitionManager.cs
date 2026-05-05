using System.Collections.Generic;

namespace NS.Framework.Authorization.Abstractions.Permissions;

public interface IPermissionDefinitionManager
{
    PermissionDefinition? GetOrNull(string name);

    IReadOnlyList<PermissionDefinition> GetPermissions();

    IReadOnlyList<PermissionGroupDefinition> GetGroups();

    IReadOnlyList<PermissionModuleDefinition> GetModules();
}

