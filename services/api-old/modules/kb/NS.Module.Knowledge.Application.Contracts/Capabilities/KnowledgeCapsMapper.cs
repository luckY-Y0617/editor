using System.Collections.Generic;
using NS.Module.Knowledge.Domain.Shared.Enums;

namespace  NS.Module.Knowledge.Application.Contracts.Capabilities;

public static class KnowledgeCapsMapper
{
    public static Dictionary<string, object?> ForRole(KnowledgeBaseMemberRole role)
    {
        // 你可以按你已有 KnowledgePermissions（kb.base.* / kb.doc.*）进一步细分
        // 这里先给一个“大厂常见的 UI 能力集”
        return role switch
        {
            KnowledgeBaseMemberRole.Owner => new Dictionary<string, object?>
            {
                ["canView"] = true,
                ["canEditKb"] = true,
                ["canDeleteKb"] = true,
                ["canManageMembers"] = true,

                ["canCreateDoc"] = true,
                ["canEditDoc"] = true,
                ["canDeleteDoc"] = true,
                ["canMoveDoc"] = true,
                ["canComment"] = true,
            },

            KnowledgeBaseMemberRole.Admin => new Dictionary<string, object?>
            {
                ["canView"] = true,
                ["canEditKb"] = true,
                ["canDeleteKb"] = false,      // 通常只有 owner 能删
                ["canManageMembers"] = true,

                ["canCreateDoc"] = true,
                ["canEditDoc"] = true,
                ["canDeleteDoc"] = true,
                ["canMoveDoc"] = true,
                ["canComment"] = true,
            },

            KnowledgeBaseMemberRole.Editor => new Dictionary<string, object?>
            {
                ["canView"] = true,
                ["canEditKb"] = false,
                ["canDeleteKb"] = false,
                ["canManageMembers"] = false,

                ["canCreateDoc"] = true,
                ["canEditDoc"] = true,
                ["canDeleteDoc"] = false,     // 常见：editor 不能删（或能删自己创建的，后续可加规则）
                ["canMoveDoc"] = false,
                ["canComment"] = true,
            },

            KnowledgeBaseMemberRole.Viewer => new Dictionary<string, object?>
            {
                ["canView"] = true,
                ["canEditKb"] = false,
                ["canDeleteKb"] = false,
                ["canManageMembers"] = false,

                ["canCreateDoc"] = false,
                ["canEditDoc"] = false,
                ["canDeleteDoc"] = false,
                ["canMoveDoc"] = false,
                ["canComment"] = false,
            },

            _ => new Dictionary<string, object?> { ["canView"] = false }
        };
    }

    public static Dictionary<string, object?> ForNotMember()
        => new Dictionary<string, object?> { ["canView"] = false };
}
