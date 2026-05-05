using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace NS.Module.Knowledge.Application.Contracts.Comments.Dtos;

public class CreateCommentRequestDto
{
    [Required]
    [StringLength(2000)] // 你按 CommentConsts.MaxContentLength 对齐即可
    public string Content { get; set; } = string.Empty;

    public Guid? ParentId { get; set; }

    /// <summary>
    /// 选区锚点（range anchor），前端原样传：
    /// { schema:1, type:'range', blockId, quote:{exact,prefix,suffix}, occurrence, hint }
    /// 也允许为 null（整段评论/无锚点评论）。
    /// </summary>
    public JsonElement? Position { get; set; }
}

