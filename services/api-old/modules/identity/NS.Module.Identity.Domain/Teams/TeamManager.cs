using System;
using System.Linq;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Identity.Domain.Shared.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Identity.Domain.Teams;

public class TeamManager : DomainService
{
    private readonly ISqlSugarRepository<Team, Guid> _teamRepository;
    private readonly ISqlSugarRepository<TeamMember, Guid> _teamMemberRepository;

    public TeamManager(
        ISqlSugarRepository<Team, Guid> teamRepository,
        ISqlSugarRepository<TeamMember, Guid> teamMemberRepository)
    {
        _teamRepository = teamRepository;
        _teamMemberRepository = teamMemberRepository;
    }

    #region Team 基本信息

    public virtual async Task<Team> CreateAsync(
        string name,
        Guid creatorUserId,
        string? description = null)
    {

        var exists = await _teamRepository.FindAsync(x => x.Name == name, includeDetails: false);

        if (exists != null)
        {
            throw new BusinessException("Knowledge:TeamNameAlreadyExists").WithData("Name", name);
        }

        var team = new Team(
            name: name,
            type: TeamType.Custom,
            description: description
        );

        await _teamRepository.InsertAsync(team, autoSave: true);

        var ownerMember = new TeamMember(
            teamId: team.Id,
            userId: creatorUserId,
            role: TeamMemberRole.Owner
        );

        await _teamMemberRepository.InsertAsync(ownerMember, autoSave: true);

        return team;
    }

    public virtual async Task<Team> UpdateAsync(
        Guid id,
        string name,
        string? description)
    {
        var team = await _teamRepository.GetAsync(id, includeDetails: true);

        // 名称变更时做唯一性校验
        if (!string.Equals(team.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            var exists = await _teamRepository.FindAsync(x => x.Id != id && x.Name == name, false);

            if (exists != null)
            {
                throw new BusinessException("Knowledge:TeamNameAlreadyExists").WithData("Name", name);
            }

            team.Name = name;
        }

        team.Description = description;

        await _teamRepository.UpdateAsync(team, autoSave: true);
        return team;
    }

    public virtual async Task DeleteAsync(Guid id)
    {
        var team = await _teamRepository.FindAsync(id, includeDetails: true);
        if (team == null)
        {
            return;
        }

        await _teamMemberRepository.DeleteAsync(m => m.TeamId == id, autoSave: false);

        await _teamRepository.DeleteAsync(team, autoSave: true);
    }

    #endregion

    #region Team 成员管理（聚合内部不变量）

    public virtual async Task<TeamMember> AddMemberAsync(Guid teamId, Guid userId, TeamMemberRole role)
    {
        var teams = await _teamRepository.GetListAsync(x => true, includeDetails: true);
        
        var team = await _teamRepository.GetAsync(teamId, includeDetails: true);

        if (team.Members.Any(m => m.UserId == userId))
        {
            throw new BusinessException("Knowledge:TeamMemberAlreadyExists")
                .WithData("TeamId", teamId)
                .WithData("UserId", userId);
        }

        if (role == TeamMemberRole.Owner)
        {
            throw new BusinessException("Knowledge:CanOnlyBeOneOwner").WithData("TeamId", teamId);
        }

        var member = new TeamMember(
            teamId: teamId,
            userId: userId,
            role: role);

        await _teamMemberRepository.InsertAsync(member, autoSave: true);

        return member;
    }

    public virtual async Task ChangeMemberRoleAsync(Guid teamId, Guid userId, TeamMemberRole newRole)
    {
        var team = await _teamRepository.GetAsync(teamId, includeDetails: true);

        var member = team.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);

        if (member == null)
        {
            throw new BusinessException("Knowledge:TeamMemberNotFound")
                .WithData("TeamId", teamId)
                .WithData("UserId", userId);
        }

        if (member.Role == newRole)
        {
            return;
        }

        if (member.Role == TeamMemberRole.Owner)
        {
            throw new BusinessException("Knowledge:CanOnlyBeOneOwner").WithData("TeamId", teamId);
        }

        member.Role = newRole;
        await _teamRepository.UpdateAsync(team, autoSave: true);
    }
    
    public virtual async Task RemoveMemberAsync(Guid teamId, Guid userId)
    {
        var team = await _teamRepository.GetAsync(teamId, includeDetails: true);


        var member = team.Members.FirstOrDefault(m => m.UserId == userId && m.IsActive);

        if (member == null)
        {
            return;
        }

        if (member.Role == TeamMemberRole.Owner)
        {
            throw new BusinessException("Knowledge:OwnerCantBeRemoved").WithData("TeamId", teamId);
        }

        member.Deactivate();
        await _teamRepository.UpdateAsync(team, autoSave: true);
    }

    /// <summary>
    /// 转移 Owner 权限：
    /// - 将当前 Owner 降级为 Admin
    /// - 将新成员提升为 Owner
    /// - 保证 Team 始终只有一个 Owner
    /// </summary>
    public virtual async Task TransferOwnershipAsync(Guid teamId, Guid newOwnerUserId)
    {
        var team = await _teamRepository.GetAsync(teamId, includeDetails: true);

        // 查找当前 Owner
        var currentOwner = team.Members.FirstOrDefault(m => m.IsActive && m.Role == TeamMemberRole.Owner);
        if (currentOwner == null)
        {
            throw new BusinessException("Knowledge:TeamOwnerNotFound")
                .WithData("TeamId", teamId);
        }

        // 查找新 Owner（必须是团队成员）
        var newOwner = team.Members.FirstOrDefault(m => m.UserId == newOwnerUserId && m.IsActive);
        if (newOwner == null)
        {
            throw new BusinessException("Knowledge:TeamMemberNotFound")
                .WithData("TeamId", teamId)
                .WithData("UserId", newOwnerUserId);
        }

        // 如果新 Owner 就是当前 Owner，无需操作
        if (currentOwner.UserId == newOwnerUserId)
        {
            return;
        }

        // 将当前 Owner 降级为 Admin
        currentOwner.Role = TeamMemberRole.Admin;

        // 将新成员提升为 Owner
        newOwner.Role = TeamMemberRole.Owner;

        await _teamRepository.UpdateAsync(team, autoSave: true);
    }

    #endregion
    
}
