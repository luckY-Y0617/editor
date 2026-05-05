// NS.Module.Knowledge.Domain.Shared/Enums/KnowledgeBaseType.cs
namespace NS.Module.Knowledge.Domain.Shared.Enums;

/// <summary>
/// 知识库类型（预留扩展）
/// </summary>
public enum KnowledgeBaseType
{
    /// <summary>普通知识库（默认）</summary>
    Normal = 0,

    /// <summary>模板库</summary>
    Template = 1,

    /// <summary>归档库</summary>
    Archive = 2
}