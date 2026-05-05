using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Comments.Dtos
{
    /// <summary>
    /// 获取评论列表输入。
    /// 支持按文档分页查询，也可按父评论获取子回复。
    /// </summary>
    public class GetCommentsInput : PagedAndSortedResultRequestDto
    {
        /// <summary>
        /// 必须指定文档 Id。
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// 父评论 Id（为空表示查询顶级评论列表）。
        /// </summary>
        public Guid? ParentId { get; set; }

        /// <summary>
        /// 是否包含子回复树。
        /// 如果为 true，则返回时会填充 Replies。
        /// </summary>
        public bool IncludeReplies { get; set; } = true;
    }
}