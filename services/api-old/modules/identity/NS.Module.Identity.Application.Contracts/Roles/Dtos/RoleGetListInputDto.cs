using NS.Module.Identity.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.Roles.Dtos;

public class RoleGetListInputDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
    public RoleTypeEnum? RoleType { get; set; }
}


