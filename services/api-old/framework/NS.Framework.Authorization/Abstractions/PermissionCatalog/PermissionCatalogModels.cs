using System.Collections.Generic;

namespace NS.Framework.Authorization.Abstractions.PermissionCatalog;

public sealed record PermissionModule(
    string Code,
    string Name,
    int Order
);

public sealed record PermissionModuleDetail(
    string Code,
    string DisplayName,
    int Order,
    IReadOnlyList<PermissionGroup> Groups
);

public sealed record PermissionItem(
    string Code,
    string DisplayName,
    PermissionGrantType GrantType,
    string? Description = null,
    int Order = 0
);

public sealed record PermissionGroup(
    string Code,
    string DisplayName,
    int Order = 0,
    string? Description = null,
    IReadOnlyList<PermissionGroup>? Children = null,
    IReadOnlyList<PermissionItem>? Permissions = null
);

public enum PermissionGrantType : byte
{
    System = 1,
    Resource = 2
}

