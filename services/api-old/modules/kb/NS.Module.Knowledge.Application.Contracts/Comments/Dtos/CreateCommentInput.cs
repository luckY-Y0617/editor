using System;
using System.ComponentModel.DataAnnotations;
using NS.Module.Knowledge.Domain.Shared.Consts;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Comments.Dtos
{
    /// <summary>
    /// 创建评论或回复的输入。
    /// </summary>
    public class CreateCommentInput : IEntityDto
    {
        /// <summary>
        /// 所属文档 Id。
        /// </summary>
        [Required]
        public Guid DocumentId { get; set; }

        /// <summary>
        /// 父评论 Id（为空表示顶级评论）。
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 评论内容。
        /// </summary>
        [Required]
        [StringLength(CommentConsts.MaxContentLength)]
        public string Content { get; set; } = default!;

        /// <summary>
        /// 锚定的 block 标识（可为空）。
        /// </summary>
        [StringLength(CommentConsts.MaxBlockIdLength)]
        public string? BlockId { get; set; }

        /// <summary>
        /// 在文档或 block 中的偏移量（可选）。
        /// </summary>
        public int? Offset { get; set; }
    }
}