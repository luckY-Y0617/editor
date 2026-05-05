using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;

namespace NS.Framework.Authorization.AspNetCore;

/// <summary>
/// 声明式权限授权（AND）：必须同时具备所有权限
/// 用法：
/// [RequireAllPermissions(SystemPermissions.Roles.Manage, SystemPermissions.Users.Manage)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequireAllPermissionsAttribute : AuthorizeAttribute
{
    public RequireAllPermissionsAttribute(params string[] permissionCodes)
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

        Policy = PermissionRequirement.PolicyAllPrefix + string.Join(PermissionRequirement.MultiSeparator, codes);
    }
}

