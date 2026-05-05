using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Framework.Authorization.AspNetCore;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Application.Contracts.Teams;
using NS.Module.Identity.Application.Contracts.Teams.Dtos;
using NS.Module.Identity.Domain.Shared.Authorization;
using NS.Module.Identity.Domain.Shared.Enums;
using NS.Module.Identity.Domain.Teams;
using NS.Module.Identity.Domain.Users;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Authorization;

namespace NS.Module.Identity.Application.Teams;

[Authorize]
[ApiController]
[Route("/api/app/teams")]
public class TeamAppService : ApplicationService, ITeamAppService
{
    private readonly ISqlSugarRepository<Team, Guid> _teamRepository;
    private readonly ISqlSugarRepository<TeamMember, Guid> _teamMemberRepository;
    private readonly IUserRepository _userRepository;
    private readonly TeamManager _teamManager;

    public TeamAppService(
        ISqlSugarRepository<Team, Guid> teamRepository,
        ISqlSugarRepository<TeamMember, Guid> teamMemberRepository,
        IUserRepository userRepository,
        TeamManager teamManager)
    {
        _teamRepository = teamRepository;
        _teamMemberRepository = teamMemberRepository;
        _userRepository = userRepository;
        _teamManager = teamManager;
    }

    #region Query (REST)

    [HttpGet("{id:guid}")]
    [RequirePermission(SystemPermissions.Teams.View)]
    public virtual async Task<TeamDto> GetAsync([FromRoute] Guid id)
    {
        var team = await _teamRepository.GetAsync(id, includeDetails: true);
        return ObjectMapper.Map<Team, TeamDto>(team);
    }

    [HttpGet]
    [RequirePermission(SystemPermissions.Teams.View)]
    public virtual async Task<PagedResultDto<TeamDto>> GetListAsync([FromQuery] TeamGetListInput input)
    {
        var query = await _teamRepository.GetQueryableAsync();

        if (!input.Filter.IsNullOrWhiteSpace())
        {
            query = query.Where(x => x.Name.Contains(input.Filter!));
        }

        if (input.Type.HasValue)
        {
            query = query.Where(x => x.Type == input.Type.Value);
        }

        query = query.OrderByDescending(x => x.CreationTime);

        RefAsync<int> total = 0;
        var list = await query.ToPageListAsync(
            input.SkipCount / input.MaxResultCount + 1,
            input.MaxResultCount,
            total
        );

        var dtos = ObjectMapper.Map<List<Team>, List<TeamDto>>(list);
        return new PagedResultDto<TeamDto>(total.Value, dtos);
    }

    [HttpGet("{id:guid}/members")]
    [RequirePermission(SystemPermissions.Teams.View)]
    public virtual async Task<List<TeamMemberDto>> GetTeamMembersAsync([FromRoute] Guid id)
    {
        var userId = CurrentUser.GetId();

        var me = await _teamMemberRepository.GetAsync(
            x => x.UserId == userId && x.TeamId == id,
            includeDetails: false
        );

        if (me.Role == TeamMemberRole.Member)
            throw new AbpAuthorizationException("Team成员不可访问用户列表");

        var members = await _teamMemberRepository.GetListAsync(x => x.TeamId == id, includeDetails: false);

        var dtos = ObjectMapper.Map<List<TeamMember>, List<TeamMemberDto>>(members);

        var userIds = members.Select(x => x.UserId).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var users = await _userRepository.GetListAsync(x => userIds.Contains(x.Id));
            var nameMap = users.ToDictionary(x => x.Id, x => x.UserName);

            foreach (var dto in dtos)
            {
                dto.Username = nameMap.GetValueOrDefault(dto.UserId, string.Empty);
            }
        }

        return dtos;
    }


    #endregion

    #region Team CRUD (REST)

    [HttpPost]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task<TeamDto> CreateAsync([FromBody] TeamCreateUpdateDto input)
    {
        var team = await _teamManager.CreateAsync(
            input.Name,
            CurrentUser.GetId(),
            input.Description
        );

        return ObjectMapper.Map<Team, TeamDto>(team);
    }

    [HttpPut("{id:guid}")]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task<TeamDto> UpdateAsync([FromRoute] Guid id, [FromBody] TeamCreateUpdateDto input)
    {
        await _teamManager.UpdateAsync(
            id,
            input.Name,
            input.Description
        );

        var fullTeam = await _teamRepository.GetAsync(id, includeDetails: true);
        return ObjectMapper.Map<Team, TeamDto>(fullTeam);
    }

    [HttpDelete("{id:guid}")]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task DeleteAsync([FromRoute] Guid id)
    {
        await _teamManager.DeleteAsync(id);
    }

    #endregion

    #region Members (REST sub-resource)

    [HttpPost("{id:guid}/members")]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task<TeamMemberDto> AddMemberAsync([FromRoute] Guid id, [FromBody] TeamAddMemberInput input)
    {
        var member = await _teamManager.AddMemberAsync(id, input.UserId, input.Role);

        return ObjectMapper.Map<TeamMember, TeamMemberDto>(member);
    }

    [HttpPut("{id:guid}/members/{userId:guid}")]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task ChangeMemberRoleAsync(
        [FromRoute] Guid id,
        [FromRoute] Guid userId,
        [FromBody] TeamChangeMemberRoleInput input)
    {
        input.TeamId = id;
        input.UserId = userId;

        await _teamManager.ChangeMemberRoleAsync(input.TeamId, input.UserId, input.Role);
    }

    [HttpDelete("{id:guid}/members/{userId:guid}")]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task RemoveMemberAsync([FromRoute] Guid id, [FromRoute] Guid userId)
    {
        await _teamManager.RemoveMemberAsync(id, userId);
    }

    [HttpPut("{id:guid}/owner")]
    [RequirePermission(SystemPermissions.Teams.Manage)]
    public virtual async Task TransferOwnershipAsync([FromRoute] Guid id, [FromBody] TeamTransferOwnershipInput input)
    {
        // REST：以 route teamId 为准
        input.TeamId = id;

        var currentUserId = CurrentUser.GetId();
        var team = await _teamRepository.GetAsync(input.TeamId, includeDetails: true);

        var currentUserMember = team.Members.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive);
        if (currentUserMember == null || currentUserMember.Role != TeamMemberRole.Owner)
        {
            throw new BusinessException("Identity:OnlyOwnerCanTransferOwnership")
                .WithData("TeamId", input.TeamId);
        }

        await _teamManager.TransferOwnershipAsync(input.TeamId, input.NewOwnerUserId);
    }

    #endregion
}
