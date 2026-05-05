using System;
using NS.Module.Identity.Domain.Shared.Enums;

namespace NS.Module.Identity.Application.Contracts.Teams.Dtos;

public class TeamAddMemberInput
{
    public Guid UserId { get; set; }

    public TeamMemberRole Role { get; set; } = TeamMemberRole.Member;
}