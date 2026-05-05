using System;
using System.Threading.Tasks;
using NS.Module.Identity.Application.Contracts.Teams.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Identity.Application.Contracts.Teams;

public interface ITeamAppService : IApplicationService
{
    Task<TeamDto> GetAsync(Guid id);

    Task<PagedResultDto<TeamDto>> GetListAsync(TeamGetListInput input);

    Task<TeamDto> CreateAsync(TeamCreateUpdateDto input);

    Task<TeamDto> UpdateAsync(Guid id, TeamCreateUpdateDto input);

    Task DeleteAsync(Guid id);

    Task<TeamMemberDto> AddMemberAsync(Guid id, TeamAddMemberInput input);

    Task ChangeMemberRoleAsync(Guid id, Guid userId, TeamChangeMemberRoleInput input);

    Task RemoveMemberAsync(Guid id, Guid userId);

    Task TransferOwnershipAsync(Guid id, TeamTransferOwnershipInput input);
}