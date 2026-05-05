using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Knowledge.Domain.KnowledgeBases;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.Members;

/// <summary>
/// 知识库成员领域服务：
/// - 负责成员增删改
/// - 保证一个 KB 至少有一个 Owner（Active Owner）
/// - 保证 (KnowledgeBaseId, UserId) 语义唯一（存在则更新，不重复插入）
/// </summary>
public class KnowledgeBaseMemberManager : DomainService
{
    private readonly IKnowledgeBaseRepository _kbRepository;
    private readonly IKnowledgeBaseMemberRepository _memberRepository;

    public KnowledgeBaseMemberManager(
        IKnowledgeBaseRepository kbRepository,
        IKnowledgeBaseMemberRepository memberRepository)
    {
        _kbRepository = kbRepository;
        _memberRepository = memberRepository;
    }

    /// <summary>
    /// 添加/更新成员：
    /// - 不存在：插入
    /// - 已存在且已激活：更新角色
    /// </summary>
    public virtual async Task<KnowledgeBaseMember> AddOrUpdateMemberAsync(
        Guid knowledgeBaseId,
        Guid userId,
        KnowledgeBaseMemberRole role,
        CancellationToken cancellationToken = default)
    {
        if (knowledgeBaseId == Guid.Empty)
        {
            throw new BusinessException("KnowledgeBase:InvalidKnowledgeBaseId")
                .WithData("KnowledgeBaseId", knowledgeBaseId);
        }

        if (userId == Guid.Empty)
        {
            throw new BusinessException("KnowledgeBase:InvalidUserId")
                .WithData("UserId", userId);
        }

        // 确认 KB 存在（GetAsync 不存在会抛异常）
        var kb = await _kbRepository.GetAsync(knowledgeBaseId, includeDetails: false, cancellationToken);

        var member = await _memberRepository.FindByBaseAndUserAsync(
            knowledgeBaseId,
            userId,
            cancellationToken);

        if (member == null)
        {
            // 新建成员：JoinedTime 一律用 Clock.Now
            member = new KnowledgeBaseMember(
                knowledgeBaseId: knowledgeBaseId,
                userId: userId,
                role: role
            );

            return await _memberRepository.InsertAsync(member, autoSave: true, cancellationToken);
        }

        if (member.Role != role)
        {
            member.ChangeRole(role);
        }

        return await _memberRepository.UpdateAsync(member, autoSave: true, cancellationToken);
    }

    /// <summary>
    /// 修改成员角色。
    /// - 不允许把最后一个 Active Owner 降级
    /// </summary>
    public virtual async Task ChangeRoleAsync(
        Guid knowledgeBaseId,
        Guid userId,
        KnowledgeBaseMemberRole newRole,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.FindByBaseAndUserAsync(
            knowledgeBaseId,
            userId,
            cancellationToken);

        if (member == null)
        {
            throw new BusinessException("KnowledgeBase:MemberNotFound")
                .WithData("KnowledgeBaseId", knowledgeBaseId)
                .WithData("UserId", userId);
        }

        // 当前是 Owner 且要降级：校验是否是最后一个 Owner
        if (member.Role == KnowledgeBaseMemberRole.Owner &&
            newRole != KnowledgeBaseMemberRole.Owner)
        {
            var allMembers = await _memberRepository.GetListByBaseAsync(knowledgeBaseId, cancellationToken);
            var activeOwnerCount = allMembers.Count(m => m.Role == KnowledgeBaseMemberRole.Owner);

            if (activeOwnerCount <= 1)
            {
                throw new BusinessException("KnowledgeBase:CannotDowngradeLastOwner")
                    .WithData("KnowledgeBaseId", knowledgeBaseId);
            }
        }

        if (member.Role != newRole)
        {
            member.ChangeRole(newRole);
            await _memberRepository.UpdateAsync(member, autoSave: true, cancellationToken);
        }
    }

    /// <summary>
    /// 移除成员（软移除：IsActive=false）。
    /// - 不允许移除最后一个 Active Owner
    /// </summary>
    public virtual async Task RemoveMemberAsync(
        Guid knowledgeBaseId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var member = await _memberRepository.FindByBaseAndUserAsync(
            knowledgeBaseId,
            userId,
            cancellationToken);

        if (member == null )
        {
            return;
        }

        if (member.Role == KnowledgeBaseMemberRole.Owner)
        {
            var allMembers = await _memberRepository.GetListByBaseAsync(knowledgeBaseId, cancellationToken);
            var activeOwnerCount = allMembers.Count(m => m.Role == KnowledgeBaseMemberRole.Owner);

            if (activeOwnerCount <= 1)
            {
                throw new BusinessException("KnowledgeBase:CannotRemoveLastOwner")
                    .WithData("KnowledgeBaseId", knowledgeBaseId);
            }
        }

        await _memberRepository.UpdateAsync(member, autoSave: true, cancellationToken);
    }

    /// <summary>
    /// 获取某个 KB 的全部有效成员。
    /// </summary>
    public virtual async Task<List<KnowledgeBaseMember>> GetActiveMembersAsync(
        Guid knowledgeBaseId,
        CancellationToken cancellationToken = default)
    {
        var members = await _memberRepository.GetListByBaseAsync(knowledgeBaseId, cancellationToken);
        return members.ToList();
    }

    /// <summary>
    /// 在创建 KB 时确保创建者一定有一条 Active Owner 成员记录。
    /// 注意：存在则 Update（Activate+Owner），不存在才 Insert，绝不重复插入。
    /// </summary>
    public virtual async Task<KnowledgeBaseMember> EnsureOwnerAsync(
        KnowledgeBase knowledgeBase,
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        if (knowledgeBase == null)
        {
            throw new BusinessException("KnowledgeBase:NullKnowledgeBase");
        }

        if (ownerUserId == Guid.Empty)
        {
            throw new BusinessException("KnowledgeBase:InvalidOwnerId")
                .WithData("OwnerId", ownerUserId);
        }

        var existing = await _memberRepository.FindByBaseAndUserAsync(
            knowledgeBase.Id,
            ownerUserId,
            cancellationToken);

        if (existing != null)
        {
            if (existing.Role != KnowledgeBaseMemberRole.Owner) existing.ChangeRole(KnowledgeBaseMemberRole.Owner);

            return await _memberRepository.UpdateAsync(existing, autoSave: true, cancellationToken);
        }

        var owner = new KnowledgeBaseMember(
            knowledgeBaseId: knowledgeBase.Id,
            userId: ownerUserId,
            role: KnowledgeBaseMemberRole.Owner
        );

        return await _memberRepository.InsertAsync(owner, autoSave: true, cancellationToken);
    }
}
