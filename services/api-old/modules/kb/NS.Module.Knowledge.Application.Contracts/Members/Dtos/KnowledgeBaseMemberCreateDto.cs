using System;
using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Application.Contracts.Members.Dtos;

/// <summary>
/// 新增/邀请成员输入
/// </summary>
public class KnowledgeBaseMemberCreateDto
{
    public Guid UserId { get; set; }
    public KnowledgeBaseMemberRole Role { get; set; }
}