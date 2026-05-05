using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Module.Knowledge.Application.Contracts.Tags.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.Knowledge.Application.Contracts.Tags;

/// <summary>
/// 标签应用服务
/// - Tag 是 KnowledgeBase 级资源
/// - 所有权限裁决统一基于 BaseId
/// </summary>
public interface ITagAppService : IApplicationService
{
    /// <summary>
    /// 获取知识库下的标签列表
    /// </summary>
    Task<PagedResultDto<TagDto>> GetListAsync(
        Guid baseId,
        GetTagListInput input);

    /// <summary>
    /// 获取热门标签
    /// </summary>
    Task<List<TagDto>> GetTopAsync(
        Guid baseId,
        GetTopTagsInput input);

    /// <summary>
    /// 获取单个标签
    /// </summary>
    Task<TagDto> GetAsync(
        Guid baseId,
        Guid tagId);

    /// <summary>
    /// 创建标签
    /// </summary>
    Task<TagDto> CreateAsync(
        Guid baseId,
        TagCreateUpdateDto input);

    /// <summary>
    /// 更新标签
    /// </summary>
    Task<TagDto> UpdateAsync(
        Guid baseId,
        Guid tagId,
        TagCreateUpdateDto input);

    /// <summary>
    /// 删除标签
    /// </summary>
    Task DeleteAsync(
        Guid baseId,
        Guid tagId);

    /// <summary>
    /// 设置文档标签
    /// </summary>
    Task<List<TagDto>> SetDocumentTagsAsync(
        Guid baseId,
        Guid documentId,
        SetDocumentTagsInput input);
}