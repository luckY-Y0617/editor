using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Application.Contracts.Documents.Dtos;
using NS.Module.Knowledge.Application.Contracts.Tags.Dtos;
using NS.Module.Knowledge.Application.Contracts.Versions;
using NS.Module.Knowledge.Application.Contracts.Versions.Dtos;
using NS.Module.Knowledge.Domain.Documents;
using NS.Module.Knowledge.Domain.Tags;
using NS.Module.Knowledge.Domain.Versions;
using NS.Module.Knowledge.Domain.Shared.Authorization;
using NS.Framework.Authorization.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;

namespace NS.Module.Knowledge.Application.Versions;

[Authorize]
[ApiController]
[Route("/api/app/kbs/{baseId:guid}/documents/{documentId:guid}/versions")]
public class DocumentVersionAppService : ApplicationService, IDocumentVersionAppService
{
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IDocumentRepository _documentRepository;
    private readonly ISqlSugarRepository<DocumentContent, Guid> _contentRepository;
    private readonly ISqlSugarRepository<DocumentTag, Guid> _documentTagRepository;
    private readonly ITagRepository _tagRepository;
    private readonly DocumentContentManager _documentContentManager;

    public DocumentVersionAppService(
        IDocumentVersionRepository versionRepository,
        IDocumentRepository documentRepository,
        ISqlSugarRepository<DocumentContent, Guid> contentRepository,
        ISqlSugarRepository<DocumentTag, Guid> documentTagRepository,
        ITagRepository tagRepository,
        DocumentContentManager documentContentManager)
    {
        _versionRepository = versionRepository;
        _documentRepository = documentRepository;
        _contentRepository = contentRepository;
        _documentTagRepository = documentTagRepository;
        _tagRepository = tagRepository;
        _documentContentManager = documentContentManager;
    }

    /// <summary>
    /// 获取文档版本列表
    /// </summary>
    [HttpGet]
    [RequirePermission(KnowledgePermissions.Version.View)]
    public virtual async Task<PagedResultDto<DocumentVersionDto>> GetListAsync(
        Guid baseId,
        Guid documentId,
        [FromQuery] GetDocumentVersionsInput input)
    {
        Check.NotNull(input, nameof(input));

        var list = await _versionRepository.GetListByDocumentIdAsync(
            documentId,
            maxCount: null);

        var totalCount = list.Count;

        var items = list
            .Skip(input.SkipCount)
            .Take(input.MaxResultCount)
            .ToList();

        var dtoList = ObjectMapper.Map<List<DocumentVersion>, List<DocumentVersionDto>>(items);

        return new PagedResultDto<DocumentVersionDto>(totalCount, dtoList);
    }

    /// <summary>
    /// 获取单个版本详情
    /// </summary>
    [HttpGet("{versionId:guid}")]
    [RequirePermission(KnowledgePermissions.Version.View)]
    public virtual async Task<DocumentVersionDto> GetAsync(
        Guid baseId,
        Guid documentId,
        Guid versionId)
    {
        var version = await _versionRepository.GetAsync(versionId, includeDetails: false);

        if (version == null || version.DocumentId != documentId)
        {
            throw new EntityNotFoundException(typeof(DocumentVersion), versionId);
        }

        return ObjectMapper.Map<DocumentVersion, DocumentVersionDto>(version);
    }

    /// <summary>
    /// 从历史版本恢复文档
    /// </summary>
    [HttpPost("{versionId:guid}/restore")]
    [RequirePermission(KnowledgePermissions.Version.Restore)]
    public virtual async Task<DocumentDetailDto> RestoreAsync(
        Guid baseId,
        Guid documentId,
        Guid versionId)
    {
        var doc = await _documentRepository.GetAsync(documentId, includeDetails: false);
        if (doc == null)
        {
            throw new EntityNotFoundException(typeof(Document), documentId);
        }

        var version = await _versionRepository.GetAsync(versionId, includeDetails: false);
        if (version == null || version.DocumentId != doc.Id)
        {
            throw new BusinessException("DocumentVersion:NotBelongToDocument");
        }

        // 核心恢复逻辑交给领域服务
        await _documentContentManager.RestoreFromVersionAsync(doc, version);

        // 重新组装文档详情
        var content = await _contentRepository.FindAsync(
            x => x.Id == doc.Id,
            includeDetails: false);

        var docTags = await _documentTagRepository.GetListAsync(
            x => x.DocumentId == doc.Id,
            includeDetails: false);

        var detail = ObjectMapper.Map<Document, DocumentDetailDto>(doc);

        if (content != null)
        {
            detail.Content = new DocumentContentDto
            {
                DocumentId = content.Id,
                ContentJson = content.ContentJson,
                ContentHtml = content.ContentHtml
            };
        }

        if (docTags.Count > 0)
        {
            var tagIds = docTags.Select(x => x.TagId).Distinct().ToList();

            var tags = await _tagRepository.GetListAsync(
                t => tagIds.Contains(t.Id),
                includeDetails: false);

            detail.Tags = ObjectMapper.Map<List<Tag>, List<TagDto>>(tags);
        }

        return detail;
    }
}
