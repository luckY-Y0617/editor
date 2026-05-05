using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Application.Contracts.Tags;
using NS.Module.Knowledge.Application.Contracts.Tags.Dtos;
using NS.Module.Knowledge.Domain.Documents;
using NS.Module.Knowledge.Domain.Tags;
using NS.Module.Knowledge.Domain.Shared.Authorization;
using NS.Framework.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;

namespace NS.Module.Knowledge.Application.Tags;

[Authorize]
[ApiController]
[Route("/api/app/kbs/{baseId:guid}/tags")]
public class TagAppService : ApplicationService, ITagAppService
{
    private readonly ITagRepository _tagRepository;
    private readonly ISqlSugarRepository<DocumentTag, Guid> _documentTagRepository;
    private readonly TagManager _tagManager;

    public TagAppService(
        ITagRepository tagRepository,
        ISqlSugarRepository<DocumentTag, Guid> documentTagRepository,
        TagManager tagManager)
    {
        _tagRepository = tagRepository;
        _documentTagRepository = documentTagRepository;
        _tagManager = tagManager;
    }

    /// <summary>
    /// 获取某知识库下的标签列表
    /// GET /api/kbs/{baseId}/tags
    /// </summary>
    [HttpGet]
    [RequirePermission(KnowledgePermissions.Tag.View)]
    public virtual async Task<PagedResultDto<TagDto>> GetListAsync(
        Guid baseId,
        [FromQuery] GetTagListInput input)
    {
        Check.NotNull(input, nameof(input));

        var tags = await _tagRepository.GetListByKnowledgeBaseIdAsync(
            baseId,
            filter: input.Filter);

        var totalCount = tags.Count;

        var items = tags
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtoList = ObjectMapper.Map<List<Tag>, List<TagDto>>(items);

        return new PagedResultDto<TagDto>(totalCount, dtoList);
    }

    /// <summary>
    /// 获取热门标签
    /// GET /api/kbs/{baseId}/tags/top
    /// </summary>
    [HttpGet("top")]
    [RequirePermission(KnowledgePermissions.Tag.View)]
    public virtual async Task<List<TagDto>> GetTopAsync(
        Guid baseId,
        [FromQuery] GetTopTagsInput input)
    {
        var tags = await _tagRepository.GetTopTagsAsync(baseId, input.MaxCount);
        return ObjectMapper.Map<List<Tag>, List<TagDto>>(tags);
    }

    /// <summary>
    /// 获取单个标签
    /// GET /api/kbs/{baseId}/tags/{tagId}
    /// </summary>
    [HttpGet("{tagId:guid}")]
    [RequirePermission(KnowledgePermissions.Tag.View)]
    public virtual async Task<TagDto> GetAsync(
        Guid baseId,
        Guid tagId)
    {
        var tag = await _tagRepository.GetAsync(tagId);
        if (tag == null || tag.IsDeleted)
        {
            throw new EntityNotFoundException(typeof(Tag), tagId);
        }

        return ObjectMapper.Map<Tag, TagDto>(tag);
    }

    /// <summary>
    /// 创建标签
    /// POST /api/kbs/{baseId}/tags
    /// </summary>
    [HttpPost]
    [RequirePermission(KnowledgePermissions.Tag.Edit)]
    public virtual async Task<TagDto> CreateAsync(
        Guid baseId,
        [FromBody] TagCreateUpdateDto input)
    {
        Check.NotNull(input, nameof(input));

        var tag = await _tagManager.CreateAsync(
            knowledgeBaseId: baseId,
            name: input.Name,
            color: input.Color,
            icon: input.Icon);

        return ObjectMapper.Map<Tag, TagDto>(tag);
    }

    /// <summary>
    /// 更新标签
    /// PUT /api/kbs/{baseId}/tags/{tagId}
    /// </summary>
    [HttpPut("{tagId:guid}")]
    [RequirePermission(KnowledgePermissions.Tag.Edit)]
    public virtual async Task<TagDto> UpdateAsync(
        Guid baseId,
        Guid tagId,
        [FromBody] TagCreateUpdateDto input)
    {
        Check.NotNull(input, nameof(input));

        var tag = await _tagRepository.GetAsync(tagId);
        if (tag == null || tag.IsDeleted)
        {
            throw new EntityNotFoundException(typeof(Tag), tagId);
        }

        tag.SetProperty(nameof(tag.Name), input.Name);
        tag.SetColor(input.Color);
        tag.SetIcon(input.Icon);

        await _tagRepository.UpdateAsync(tag, autoSave: true);

        return ObjectMapper.Map<Tag, TagDto>(tag);
    }

    /// <summary>
    /// 删除标签
    /// DELETE /api/kbs/{baseId}/tags/{tagId}
    /// </summary>
    [HttpDelete("{tagId:guid}")]
    [RequirePermission(KnowledgePermissions.Tag.Edit)]
    public virtual async Task DeleteAsync(
        Guid baseId,
        Guid tagId)
    {
        var tag = await _tagRepository.GetAsync(tagId);
        if (tag == null || tag.IsDeleted)
        {
            return;
        }

        await _tagManager.DeleteAsync(tag);
    }

    /// <summary>
    /// 设置文档标签
    /// POST /api/kbs/{baseId}/documents/{documentId}/tags
    /// </summary>
    [HttpPost("/api/kbs/{baseId:guid}/documents/{documentId:guid}/tags")]
    [RequirePermission(KnowledgePermissions.Tag.Edit)]
    public virtual async Task<List<TagDto>> SetDocumentTagsAsync(
        Guid baseId,
        Guid documentId,
        [FromBody] SetDocumentTagsInput input)
    {
        Check.NotNull(input, nameof(input));

        // 这里只需要 documentId，领域内自行校验归属
        var docPlaceholder = new Document(
            knowledgeBaseId: baseId,
            title: "placeholder",
            parentId: null);

        await _tagManager.SetTagsForDocumentAsync(docPlaceholder, input.TagIds);

        var docTags = await _documentTagRepository.GetListAsync(
            x => x.DocumentId == documentId);

        if (docTags.Count == 0)
        {
            return new List<TagDto>();
        }

        var tagIds = docTags.Select(x => x.TagId).Distinct().ToList();

        var tags = await _tagRepository.GetListAsync(
            t => tagIds.Contains(t.Id),
            includeDetails: false);

        return ObjectMapper.Map<List<Tag>, List<TagDto>>(tags);
    }
}
