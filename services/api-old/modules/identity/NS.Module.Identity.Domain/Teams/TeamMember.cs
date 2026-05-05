using System;
using NS.Module.Identity.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Identity.Domain.Teams;

[SugarTable("id_team_members")]
public class TeamMember : FullAuditedAggregateRoot<Guid>, IMultiTenant
{

    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }

    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;

    /// <summary>是否活跃</summary>
    public bool IsActive { get; set; } = true;


    [Navigate(NavigateType.ManyToOne, nameof(TeamId))]
    public Team? Team { get; set; }

    public TeamMember() { }

    public TeamMember( Guid teamId, Guid userId, TeamMemberRole role)
    {
        TeamId = teamId;
        UserId = userId;
        Role = role;
    }
    
    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}