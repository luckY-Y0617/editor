using System;
using Microsoft.AspNetCore.Authorization;

namespace NS.Framework.Authorization.AspNetCore;

/// <summary>
/// 声明式权限授权：
/// 用法：
/// [RequirePermission(SystemPermissions.Roles.Manage)]
/// 等价于：
/// [Authorize(Policy = PermissionRequirement.PolicyPrefix + SystemPermissions.Roles.Manage)]
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permissionCode)
    {
        if (string.IsNullOrWhiteSpace(permissionCode))
        {
            throw new ArgumentException("permissionCode cannot be null or whitespace.", nameof(permissionCode));
        }

        Policy = PermissionRequirement.PolicyPrefix + permissionCode.Trim();
    }
}