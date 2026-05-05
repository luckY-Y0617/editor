namespace NS.Module.Knowledge.Domain.Shared.Enums;

/// <summary>
/// 知识库可见性（类似语雀：私密 / 团队 / 公开）
/// </summary>
public enum KnowledgeBaseVisibility : byte
{
    Private = 0, // Owner + 指定成员
    Team    = 1, // 团队内所有成员自动可见
    Public  = 2  // 租户内所有用户自动可见
}
