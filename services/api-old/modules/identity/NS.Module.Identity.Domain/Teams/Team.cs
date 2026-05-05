using System;
using System.Collections.Generic;
using NS.Module.Identity.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Identity.Domain.Teams;

[SugarTable("id_teams")]
public class Team : FullAuditedAggregateRoot<Guid>, IMultiTenant
{

    [SugarColumn(IsNullable = true)]
    public Guid? TenantId { get; set; }

    [SugarColumn(Length = 128, IsNullable = false)]
    public string Name { get; set; } = null!;

    [SugarColumn(Length = 512, IsNullable = true)]
    public string? Description { get; set; }

    public TeamType Type { get; set; } = TeamType.Custom;

    [Navigate(NavigateType.OneToMany, nameof(TeamMember.TeamId))]
    public List<TeamMember> Members { get; set; } = null!;

    public Team() { }

    public Team(string name, TeamType type, string? description)
    {
        Name = name;
        Type = type;
        Description = description;
    }
}