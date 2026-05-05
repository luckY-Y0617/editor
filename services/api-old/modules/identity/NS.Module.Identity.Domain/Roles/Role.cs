using System;
using System.Collections.Generic;
using SqlSugar;
using Volo.Abp.MultiTenancy;
using Check = Volo.Abp.Check;
using Volo.Abp.Domain.Entities.Auditing;
using NS.Framework.Core.Abstractions.Domain;
using NS.Module.Identity.Domain.Shared.Consts;
using NS.Module.Identity.Domain.Shared.Enums;


namespace NS.Module.Identity.Domain.Roles;

[SugarTable("id_roles")]
[SugarIndex("IX_Roles_RoleCode", nameof(RoleCode), OrderByType.Asc, true)]
[SugarIndex("IX_Roles_NormalizedRoleName", nameof(NormalizedRoleName), OrderByType.Asc)]
[SugarIndex("IX_Roles_Tenant", nameof(TenantId), OrderByType.Asc)]
public class Role : FullAuditedAggregateRoot<Guid>, IMultiTenant, IOrderNum
{
    public string RoleName { get; private set; } = string.Empty;
    public string NormalizedRoleName { get; private set; } = string.Empty;
    public string RoleCode { get; private set; } = string.Empty;
    
    [SugarColumn(IsNullable = true)]
    public string? Description { get; private set; }
    public RoleTypeEnum RoleType { get; private set; } = RoleTypeEnum.Custom;
    public int OrderNum { get; private set; }
    public bool IsActive { get; private set; } = true;
    public Guid? TenantId { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(RolePermission.RoleId))]
    public List<RolePermission> Permissions { get; set; } = null!;
    
    public Role()
    {
    }

    public Role(string roleName, string roleCode)
    {
        SetRoleName(roleName);
        SetRoleCode(roleCode);
    }

    public void SetRoleName(string roleName)
    {
        Check.NotNullOrWhiteSpace(roleName, nameof(roleName), RoleConsts.RoleNameMaxLength);
        RoleName = roleName;
        NormalizedRoleName = roleName.ToUpperInvariant();
    }

    public void SetRoleCode(string roleCode)
    {
        Check.NotNullOrWhiteSpace(roleCode, nameof(roleCode), RoleConsts.RoleCodeMaxLength);
        RoleCode = roleCode;
    }

    public void SetDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            Description = null;
            return;
        }

        Check.Length(description, nameof(description), RoleConsts.DescriptionMaxLength);
        Description = description;
    }

    public void SetRoleType(RoleTypeEnum roleType)
    {
        RoleType = roleType;
    }

    public void SetOrder(int orderNum)
    {
        OrderNum = orderNum;
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}


