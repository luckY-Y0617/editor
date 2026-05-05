using System;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Members.Dtos;

/// <summary>
/// 成员列表 / 管理界面用的成员信息
/// </summary>
public class KnowledgeBaseMemberDto : EntityDto<Guid>
{
    public Guid KnowledgeBaseId { get; set; }
    public Guid UserId { get; set; }
    public KnowledgeBaseMemberRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreationTime { get; set; }

    public string? UserName { get; set; }
}