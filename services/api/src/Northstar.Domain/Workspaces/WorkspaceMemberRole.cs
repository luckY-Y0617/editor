using Northstar.Domain.Security;

namespace Northstar.Domain.Workspaces;

public static class WorkspaceMemberRole
{
    public const string Owner = PermissionRole.Owner;
    public const string Admin = PermissionRole.Admin;
    public const string Editor = PermissionRole.Editor;
    public const string Viewer = PermissionRole.Viewer;

    public static bool IsValid(string? role)
    {
        return role is Owner or Admin or Editor or Viewer;
    }
}
