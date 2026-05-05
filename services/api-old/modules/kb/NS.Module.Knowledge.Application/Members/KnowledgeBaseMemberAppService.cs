using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.Authorization.AspNetCore;
using NS.Module.Identity.Application.Contracts.Users;
using NS.Module.Knowledge.Application.Contracts.Members;
using NS.Module.Knowledge.Application.Contracts.Members.Dtos;
using NS.Module.Knowledge.Domain.Members;
using NS.Module.Knowledge.Domain.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Knowledge.Application.Members;

[Authorize]
[ApiController]
[Route("/api/app/kbs/{baseId:guid}/members")]
public class KnowledgeBaseMemberAppService
    : ApplicationService,
      IKnowledgeBaseMemberAppService
{
    private readonly IKnowledgeBaseMemberRepository _memberRepository;
    private readonly KnowledgeBaseMemberManager _memberManager;
    private readonly IAuthUserProfileProvider _userProfileProvider;

    public KnowledgeBaseMemberAppService(
        IKnowledgeBaseMemberRepository memberRepository,
        KnowledgeBaseMemberManager memberManager,
        IAuthUserProfileProvider userProfileProvider)
    {
        _memberRepository = memberRepository;
        _memberManager = memberManager;
        _userProfileProvider = userProfileProvider;
    }

    [HttpGet]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.MemberManage)]
    public virtual async Task<ListResultDto<KnowledgeBaseMemberDto>> GetListAsync(
        [FromRoute(Name = "baseId")] Guid baseId,
        CancellationToken cancellationToken = default)
    {
        var members = await _memberRepository.GetListByBaseAsync(baseId, cancellationToken);

        var ordered = members
            .OrderByDescending(m => m.Role)
            .ThenBy(m => m.CreationTime)
            .ToList();

        var dtos = ObjectMapper.Map<List<KnowledgeBaseMember>, List<KnowledgeBaseMemberDto>>(ordered);

        var userIds = dtos.Select(x => x.UserId).Distinct().ToList();
        if (userIds.Count > 0)
        {
            var nameMap = await _userProfileProvider.FindByIdsAsync(userIds);

            foreach (var dto in dtos)
            {
                dto.UserName = nameMap?.Where(x => x.Id == dto.UserId).First().UserName;
            }
        }

        return new ListResultDto<KnowledgeBaseMemberDto>(dtos);
    }

    [HttpPut]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.MemberManage)]
    public virtual async Task<KnowledgeBaseMemberDto> AddOrUpdateAsync(
        [FromRoute(Name = "baseId")] Guid baseId,
        [FromBody] KnowledgeBaseMemberCreateDto input,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberManager.AddOrUpdateMemberAsync(
            baseId,
            input.UserId,
            input.Role,
            cancellationToken);

        return ObjectMapper.Map<KnowledgeBaseMember, KnowledgeBaseMemberDto>(member);
    }

    [HttpPut("{userId:guid}/role")]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.MemberManage)]
    public virtual async Task ChangeRoleAsync(
        [FromRoute(Name = "baseId")] Guid knowledgeBaseId,
        [FromRoute] Guid userId,
        [FromBody] KnowledgeBaseMemberUpdateRoleDto input,
        CancellationToken cancellationToken = default)
    {

        await _memberManager.ChangeRoleAsync(
            knowledgeBaseId,
            userId,
            input.Role,
            cancellationToken);
    }

    /// <summary>
    /// DELETE /api/kbs/{baseId}/members/{userId}
    /// </summary>
    [HttpDelete("{userId:guid}")]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.MemberManage)]
    public virtual async Task RemoveAsync(
        [FromRoute(Name = "baseId")] Guid knowledgeBaseId,
        [FromRoute] Guid userId,
        CancellationToken cancellationToken = default)
    {

        await _memberManager.RemoveMemberAsync(
            knowledgeBaseId,
            userId,
            cancellationToken);
    }

}
