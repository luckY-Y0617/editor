using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace NS.Framework.Authorization.AspNetCore;

/// <summary>
/// 声明式权限授权（OR）：具备任意一个权限即可
/// 用法：
/// [RequireAnyPermissions(SystemPermissions.Roles.Manage, SystemPermissions.Roles.View)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireAnyPermissionsAttribute : AuthorizeAttribute
{
    public RequireAnyPermissionsAttribute(params string[] permissionCodes)
    {
        if (permissionCodes == null || permissionCodes.Length == 0)
        {
            throw new ArgumentException("permissionCodes cannot be empty.", nameof(permissionCodes));
        }

        var codes = permissionCodes
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (codes.Length == 0)
        {
            throw new ArgumentException("permissionCodes cannot be empty.", nameof(permissionCodes));
        }

        Policy = PermissionRequirement.PolicyAnyPrefix + string.Join(PermissionRequirement.MultiSeparator, codes);
    }
}
