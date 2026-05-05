using NS.Module.Identity.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.Teams.Dtos;

public class TeamGetListInput : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }

    public TeamType? Type { get; set; }
}