using System;
using NS.Module.Identity.Domain.Shared.Enums;

namespace NS.Module.Identity.Application.Contracts.Teams.Dtos;

public class TeamChangeMemberRoleInput
{
    public Guid TeamId { get; set; }

    public Guid UserId { get; set; }

    public TeamMemberRole Role { get; set; }
}