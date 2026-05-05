using System;
using System.Collections.Generic;
using System.Text.Json;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Comments.Dtos
{
    /// <summary>
    /// 评论传输对象。
    /// </summary>
    public class CommentDto : EntityDto<Guid>
    {
        public Guid DocumentId { get; set; }

        public Guid? ParentId { get; set; }

        public string Content { get; set; } = string.Empty;

        public Guid? CreatorId { get; set; }

        public string? CreatorName { get; set; }

        public string? AvatarUrl { get; set; }

        public DateTime CreationTime { get; set; }

        /// <summary>
        /// 原样返回 position（Json），前端用于 locateRangeAnchor()
        /// </summary>
        public JsonElement? Position { get; set; }

        public List<CommentDto> Replies { get; set; } = new();
    }
}