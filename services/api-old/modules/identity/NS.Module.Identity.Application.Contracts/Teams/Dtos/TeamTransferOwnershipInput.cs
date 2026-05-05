using System;

namespace NS.Module.Identity.Application.Contracts.Teams.Dtos;

public class TeamTransferOwnershipInput
{
    public Guid TeamId { get; set; }

    public Guid NewOwnerUserId { get; set; }
}

