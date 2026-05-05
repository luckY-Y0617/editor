using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.Knowledge.Application.Contracts.Comments.Dtos
{
    /// <summary>
    /// 点赞切换的结果。
    /// </summary>
    public class ToggleCommentLikeResultDto
    {
        public Guid Id { get; set; }
        
        /// <summary>
        /// 当前用户是否已点赞。
        /// </summary>
        public bool LikedByCurrentUser { get; set; }
        
    }
}