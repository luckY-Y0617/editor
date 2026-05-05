using System;
using NS.Module.Knowledge.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Knowledge.Domain.Members;

[SugarTable("kb_members")]
public class KnowledgeBaseMember : FullAuditedAggregateRoot<Guid>, IMultiTenant
{
    public Guid? TenantId { get; protected set; }

    /// <summary>所属知识库 Id</summary>
    public Guid KnowledgeBaseId { get; protected set; }

    /// <summary>用户 Id</summary>
    public Guid UserId { get; protected set; }

    /// <summary>角色（owner/admin/editor/viewer）</summary>
    public KnowledgeBaseMemberRole Role { get; protected set; }

    public KnowledgeBaseMember()
    {
    }

    public KnowledgeBaseMember(
        Guid knowledgeBaseId,
        Guid userId,
        KnowledgeBaseMemberRole role)
    {
        KnowledgeBaseId = knowledgeBaseId;
        UserId = userId;
        Role = role;
    }

    public void ChangeRole(KnowledgeBaseMemberRole role)
    {
        Role = role;
    }
}
