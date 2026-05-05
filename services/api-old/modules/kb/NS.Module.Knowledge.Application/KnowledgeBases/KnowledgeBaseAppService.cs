using NS.Framework.Authorization.AspNetCore;
using NS.Module.Knowledge.Domain.Shared.Authorization;
using NS.Module.Knowledge.Application.Contracts.KnowledgeBases;
using NS.Module.Knowledge.Application.Contracts.KnowledgeBases.Dtos;
using NS.Module.Knowledge.Domain.KnowledgeBases;
using NS.Module.Knowledge.Domain.Members;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.Domain.Entities;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;
using Check = Volo.Abp.Check;

namespace NS.Module.Knowledge.Application.KnowledgeBases;

[Authorize]
[ApiController]
[Route("/api/app/kbs")]
public class KnowledgeBaseAppService : ApplicationService, IKnowledgeBaseAppService
{
    private readonly IKnowledgeBaseRepository _kbRepository;
    private readonly KnowledgeBaseManager _kbManager;
    private readonly KnowledgeBaseMemberManager _memberManager;
    private readonly ILocalEventBus _localEventBus;

    public KnowledgeBaseAppService(
        IKnowledgeBaseRepository kbRepository,
        KnowledgeBaseManager kbManager,
        KnowledgeBaseMemberManager memberManager,
        ILocalEventBus localEventBus)
    {
        _kbRepository = kbRepository;
        _kbManager = kbManager;
        _memberManager = memberManager;
        _localEventBus = localEventBus;
    }

    [HttpGet]
    public virtual async Task<List<KnowledgeBaseDto>> GetListAsync(
        [FromQuery] KnowledgeBaseGetListInput input,
        CancellationToken ct = default)
    {
        // Todo：teamscope（保持原样）
        var userId = CurrentUser.GetId();

        Guid? currentTeamId = string.IsNullOrWhiteSpace(input.TeamId)
            ? null
            : Guid.Parse(input.TeamId);

        var query = await _kbRepository.GetQueryableAsync();

        var list = await query
            .Where(kb =>
                SqlFunc.Subqueryable<KnowledgeBaseMember>()
                    .Where(m =>
                        m.KnowledgeBaseId == kb.Id &&
                        m.UserId == userId
                    )
                    .Any()
            )
            .Where(x => x.TeamId == currentTeamId)
            .ToListAsync(ct);

        return ObjectMapper.Map<List<KnowledgeBase>, List<KnowledgeBaseDto>>(list);
    }

    [HttpGet("{baseId:guid}")]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.View)]
    public virtual async Task<KnowledgeBaseContextDto> GetAsync(
        [FromRoute] Guid baseId,
        CancellationToken cancellationToken = default)
    {
        var kb = await _kbRepository.GetAsync(baseId, includeDetails: true, cancellationToken);

        if (kb == null)
        {
            throw new EntityNotFoundException(typeof(KnowledgeBase), baseId);
        }

        var userId = CurrentUser.GetId();

        var member = kb.Members.SingleOrDefault(m => m.UserId == userId);

        var isMember = member is not null;

        // 私密知识库仅成员可见
        if (!isMember && kb.Visibility == KnowledgeBaseVisibility.Private)
        {
            throw new AbpAuthorizationException("KnowledgeBase is private.");
        }

        return new KnowledgeBaseContextDto
        {
            KnowledgeBase = ObjectMapper.Map<KnowledgeBase, KnowledgeBaseDto>(kb),
            IsMember = isMember,
            CurrentUserRole = isMember ? member!.Role : null
        };
    }

    [HttpPost]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.Create)]
    public virtual async Task<KnowledgeBaseDto> CreateAsync(
        [FromBody] KnowledgeBaseCreateUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(input, nameof(input));

        var userId = CurrentUser.GetId();

        var kb = await _kbManager.CreateAsync(
            name: input.Name,
            ownerId: userId,
            teamId: string.IsNullOrWhiteSpace(input.TeamId) ? null : Guid.Parse(input.TeamId),
            description: input.Description,
            icon: input.Icon,
            allowMembersCreateDoc: input.AllowMembersCreateDoc,
            visibility: input.Visibility,
            cancellationToken: cancellationToken);

        await _memberManager.EnsureOwnerAsync(
            knowledgeBase: kb,
            ownerUserId: userId,
            cancellationToken: cancellationToken);


        return ObjectMapper.Map<KnowledgeBase, KnowledgeBaseDto>(kb);
    }

    [HttpPut("{baseId:guid}")]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.Manage)]
    public virtual async Task<KnowledgeBaseDto> UpdateAsync(
        [FromRoute] Guid baseId,
        [FromBody] KnowledgeBaseCreateUpdateDto input,
        CancellationToken cancellationToken = default)
    {
        var kb = await _kbRepository.GetAsync(baseId, includeDetails: false, cancellationToken);

        if (kb == null || kb.IsDeleted)
        {
            throw new EntityNotFoundException(typeof(KnowledgeBase), baseId);
        }

        await _kbManager.UpdateAsync(
            kb,
            name: input.Name,
            description: input.Description,
            visibility: input.Visibility,
            cancellationToken: cancellationToken);

        if (!input.Icon.IsNullOrWhiteSpace())
        {
            kb.SetIcon(input.Icon);
        }

        await _kbRepository.UpdateAsync(kb, autoSave: true, cancellationToken);

        return ObjectMapper.Map<KnowledgeBase, KnowledgeBaseDto>(kb);
    }

    [HttpDelete("{baseId:guid}")]
    [RequirePermission(KnowledgePermissions.KnowledgeBase.Delete)]
    public virtual async Task DeleteAsync(
        [FromRoute] Guid baseId,
        CancellationToken cancellationToken = default)
    {
        var kb = await _kbRepository.FindAsync(baseId, includeDetails: false, cancellationToken);

        if (kb == null || kb.IsDeleted)
        {
            return;
        }

        await _kbManager.DeleteAsync(kb, cancellationToken);
    }
}
