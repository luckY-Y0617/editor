using NS.Module.Identity.Domain.Shared.Enums;

namespace NS.Module.Identity.Application.Contracts.Teams.Dtos;

public class TeamCreateUpdateDto
{
    public string Name { get; set; } = default!;

    public string? Description { get; set; }

}