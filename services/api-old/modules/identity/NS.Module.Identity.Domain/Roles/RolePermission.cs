using System;
using NS.Module.Identity.Domain.Shared.Consts;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;
using Check = Volo.Abp.Check;

namespace NS.Module.Identity.Domain.Roles;

[SugarTable("id_role_permissions")]
[SugarIndex("IX_RolePermissions_Role", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_RolePermissions_Code", nameof(PermissionCode), OrderByType.Asc)]
[SugarIndex("IX_RolePermissions_Tenant", nameof(TenantId), OrderByType.Asc)]
public class RolePermission : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid RoleId { get; private set; }
    public string PermissionCode { get; private set; } = string.Empty;
    public Guid? TenantId { get; set; }
    
    public RolePermission() { }

    public RolePermission(Guid roleId, string permissionCode)
    {
        RoleId = roleId;
        SetPermissionCode(permissionCode);
    }

    private void SetPermissionCode(string permissionCode)
    {
        Check.NotNullOrWhiteSpace(
            permissionCode,
            nameof(permissionCode),
            PermissionConsts.PermissionCodeMaxLength);
        PermissionCode = permissionCode;
    }
}

