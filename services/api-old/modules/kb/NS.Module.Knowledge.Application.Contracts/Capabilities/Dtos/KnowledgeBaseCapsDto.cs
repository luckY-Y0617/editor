using System;
using System.Collections.Generic;
using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Application.Contracts.Capabilities.Dtos;

public class KnowledgeBaseCapsDto
{
    public Guid KnowledgeBaseId { get; set; }

    /// <summary>用户在该 KB 下的角色（owner/admin/editor/viewer）；若不是成员可为空</summary>
    public KnowledgeBaseMemberRole? Role { get; set; }

    /// <summary>是否有效成员</summary>
    public bool IsActiveMember { get; set; }

    /// <summary>能力键值对（前端按钮显隐/交互判断）</summary>
    public Dictionary<string, object?> Caps { get; set; } = new();
}