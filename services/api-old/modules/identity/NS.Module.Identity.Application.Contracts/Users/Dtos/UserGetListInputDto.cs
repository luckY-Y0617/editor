using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.Users.Dtos;

public class UserGetListInputDto : PagedAndSortedResultRequestDto
{
    public string? Filter { get; set; }
}


