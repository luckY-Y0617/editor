using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Module.Knowledge.Application.Contracts.Comments.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Knowledge.Application.Contracts.Comments;

/// <summary>
/// 文档评论应用服务
/// - Comment 是 Document 的子资源
/// - 权限裁决统一基于 KnowledgeBase（BaseId）
/// </summary>
public interface ICommentAppService : IApplicationService
{
    /// <summary>
    /// 获取文档下的评论树
    /// </summary>
    Task<List<CommentDto>> GetListAsync(
        Guid baseId,
        Guid documentId);

    /// <summary>
    /// 创建评论或回复
    /// </summary>
    Task<CommentDto> CreateAsync(
        Guid baseId,
        Guid documentId,
        CreateCommentRequestDto input);

    /// <summary>
    /// 删除评论（及其子回复）
    /// </summary>
    Task DeleteAsync(
        Guid baseId,
        Guid documentId,
        Guid id);

}