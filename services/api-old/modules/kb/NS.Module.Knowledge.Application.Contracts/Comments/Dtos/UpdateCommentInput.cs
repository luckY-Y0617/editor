using System;
using System.ComponentModel.DataAnnotations;
using NS.Module.Knowledge.Domain.Shared.Consts;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Comments.Dtos
{
    /// <summary>
    /// 修改评论内容输入。
    /// 一般只允许作者或管理员调用。
    /// </summary>
    public class UpdateCommentInput : IEntityDto<Guid>
    {
        [Required]
        public Guid Id { get; set; }

        [Required]
        [StringLength(CommentConsts.MaxContentLength)]
        public string Content { get; set; } = default!;
    }
}