namespace Northstar.Domain.Security;

public static class ScopedPermissionRoles
{
    public static bool IsSupported(string? role)
    {
        return role is PermissionRole.Owner or
            PermissionRole.Admin or
            PermissionRole.Editor or
            PermissionRole.Commenter or
            PermissionRole.Viewer;
    }

    public static bool IsSupportedLinkRole(string? role)
    {
        return role is null or PermissionRole.Viewer or PermissionRole.Commenter;
    }
}
