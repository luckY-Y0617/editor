using System;
using NS.Module.Identity.Domain.Roles;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Identity.Domain.Users;

[SugarTable("id_user_roles")]
[SugarIndex("IX_UserRoles_User", nameof(UserId), OrderByType.Asc)]
[SugarIndex("IX_UserRoles_Role", nameof(RoleId), OrderByType.Asc)]
[SugarIndex("IX_UserRoles_Tenant", nameof(TenantId), OrderByType.Asc)]
public class UserRole : CreationAuditedEntity<Guid>, IMultiTenant
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public Guid? TenantId { get; set; }
    
    // ===== 多对一：User =====
    [Navigate(NavigateType.ManyToOne, nameof(UserId))]
    public User User { get; private set; } 

    // ===== 多对一：Role =====
    [Navigate(NavigateType.ManyToOne, nameof(RoleId))]
    public Role Role { get; private set; }
 
    public UserRole()
    {
    }

    public UserRole( Guid userId, Guid roleId)
    {
        UserId = userId;
        RoleId = roleId;
    }

    public void SetUser(Guid userId)
    {
        UserId = userId;
    }

    public void SetRole(Guid roleId)
    {
        RoleId = roleId;
    }
}


