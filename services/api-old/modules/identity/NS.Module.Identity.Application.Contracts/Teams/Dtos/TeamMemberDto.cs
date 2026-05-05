using System;
using NS.Module.Identity.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.Teams.Dtos;

public class TeamMemberDto : EntityDto<Guid>
{
    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }
    
    public string? Username { get; set; }
    
    public DateTime? CreationTime { get; set; }

    public TeamMemberRole Role { get; set; }

}