namespace NS.Module.Knowledge.Domain.Shared.Enums;

/// <summary>
/// 评论锚点状态。
/// 表示评论当前是否还能准确定位到文档内容中。
/// </summary>
public enum CommentStatus
{
    /// <summary>
    /// 锚点有效，能够准确定位到文档中的文本位置。
    /// （Block 存在，AnchorText / Offset 匹配成功）
    /// </summary>
    Anchored = 0,

    /// <summary>
    /// 锚点丢失：
    /// - Block 仍然存在
    /// - 但 AnchorText / Offset 已无法匹配
    /// 通常由内容被替换、删除、重排导致。
    /// </summary>
    Orphaned = 1,

    /// <summary>
    /// 所属 Block 已被删除：
    /// - BlockId 在文档结构中不存在
    /// - 评论无法再关联到任何具体内容
    /// </summary>
    BlockDeleted = 2
}