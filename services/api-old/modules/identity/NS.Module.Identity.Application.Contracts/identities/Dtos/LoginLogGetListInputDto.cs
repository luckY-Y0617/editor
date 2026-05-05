using Volo.Abp.Application.Dtos;

namespace NS.Module.Identity.Application.Contracts.identities.Dtos;

public sealed class LoginLogGetListInputDto : PagedAndSortedResultRequestDto
{
    public string? UserId { get; set; }

    public string? UserName { get; set; }

    public int? LoginStatus { get; set; }

    public string? StartTime { get; set; }

    public string? EndTime { get; set; }
}

