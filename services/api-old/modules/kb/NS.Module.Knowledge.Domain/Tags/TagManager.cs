using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Documents;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.Tags;

public class TagManager : DomainService
{
    private readonly ITagRepository _tagRepository;
    private readonly ISqlSugarRepository<DocumentTag, Guid> _documentTagRepository;

    public TagManager(
        ITagRepository tagRepository,
        ISqlSugarRepository<DocumentTag, Guid> documentTagRepository)
    {
        _tagRepository = tagRepository;
        _documentTagRepository = documentTagRepository;
    }

    /// <summary>
    /// 统一规范化：Trim + Upper（实现“同名不区分大小写/首尾空格”）
    /// </summary>
    protected virtual string NormalizeName(string name)
        => name.Trim().ToUpperInvariant();

    public virtual async Task<Tag> CreateAsync(
        Guid knowledgeBaseId,
        string name,
        string? color = null,
        string? icon = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));

        var normalized = NormalizeName(name);

        var exists = await _tagRepository.FindAsync(
            x => x.KnowledgeBaseId == knowledgeBaseId && x.Name == normalized,
            includeDetails: false,
            cancellationToken: cancellationToken);
        if (exists != null)
        {
            throw new BusinessException("Tag:NameExists")
                .WithData("KnowledgeBaseId", knowledgeBaseId)
                .WithData("Name", normalized);
        }

        var tag = new Tag(
            knowledgeBaseId: knowledgeBaseId,
            name: normalized);

        tag.SetColor(color);
        tag.SetIcon(icon);

        return await _tagRepository.InsertAsync(tag, autoSave: true, cancellationToken);
    }

    public virtual async Task RenameAsync(
        Tag tag,
        string newName,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(tag, nameof(tag));
        Check.NotNullOrWhiteSpace(newName, nameof(newName));

        var normalized = NormalizeName(newName);

        // 同 KB 下唯一（排除自己）
        var exists = await _tagRepository.FindAsync(
            x => x.KnowledgeBaseId == tag.KnowledgeBaseId && x.Name == normalized,
            includeDetails: false,
            cancellationToken: cancellationToken);
        if (exists != null && exists.Id != tag.Id)
        {
            throw new BusinessException("Tag:NameExists")
                .WithData("KnowledgeBaseId", tag.KnowledgeBaseId)
                .WithData("Name", normalized);
        }

        tag.Rename(normalized);

        await _tagRepository.UpdateAsync(tag, autoSave: true, cancellationToken);
    }

    public virtual async Task UpdateStyleAsync(
        Tag tag,
        string? color,
        string? icon,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(tag, nameof(tag));

        tag.SetColor(color);
        tag.SetIcon(icon);

        await _tagRepository.UpdateAsync(tag, autoSave: true, cancellationToken);
    }

    public virtual async Task DeleteAsync(
        Tag tag,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(tag, nameof(tag));

        // 先清理关联（DocumentTag）
        // 说明：当前 ISqlSugarRepository 未必提供批量删除 API，这里用“先查再删”的可靠写法；
        //      后续你可以把这一段下沉到 Repository 用 Deleteable 一条 SQL 做掉。
        var rels = await _documentTagRepository.GetListAsync(
            x => x.TagId == tag.Id,
            includeDetails: false,
            cancellationToken: cancellationToken);

        if (rels.Count > 0)
        {
            foreach (var rel in rels)
            {
                await _documentTagRepository.DeleteAsync(rel, autoSave: false, cancellationToken);
            }
        }

        // 再删 Tag（由上层/UnitOfWork 一次性提交更好；这里按你当前风格 autoSave=true）
        await _tagRepository.DeleteAsync(tag, autoSave: true, cancellationToken);
    }

    /// <summary>
    /// 重新设置某文档的标签集合。输入：目标 TagId 列表。
    /// 约束：
    /// - 所有 Tag 必须存在
    /// - 所有 Tag 必须属于 doc.KnowledgeBaseId（禁止跨 KB 绑定）
    /// </summary>
    public virtual async Task SetTagsForDocumentAsync(
        Document doc,
        IEnumerable<Guid>? tagIds,
        CancellationToken cancellationToken = default)
    {
        Check.NotNull(doc, nameof(doc));

        var targetIds = tagIds?.Where(x => x != Guid.Empty).Distinct().ToList()
                       ?? new List<Guid>();

        // 0) 校验 Tag 归属 KB、且全部存在
        if (targetIds.Count > 0)
        {
            var tags = await _tagRepository.GetListAsync(
                t => targetIds.Contains(t.Id),
                includeDetails: false,
                cancellationToken: cancellationToken);

            if (tags.Count != targetIds.Count)
            {
                var found = tags.Select(x => x.Id).ToHashSet();
                var missing = targetIds.Where(id => !found.Contains(id)).ToList();

                throw new BusinessException("Tag:NotFound")
                    .WithData("MissingTagIds", missing);
            }

            var invalid = tags.Where(t => t.KnowledgeBaseId != doc.KnowledgeBaseId).ToList();
            if (invalid.Count > 0)
            {
                throw new BusinessException("Tag:CrossKnowledgeBaseBindingNotAllowed")
                    .WithData("DocumentId", doc.Id)
                    .WithData("DocumentKnowledgeBaseId", doc.KnowledgeBaseId)
                    .WithData("InvalidTagIds", invalid.Select(x => x.Id).ToList());
            }
        }

        // 1) 获取当前文档已有的标签关联
        var existing = await _documentTagRepository.GetListAsync(
            x => x.DocumentId == doc.Id,
            includeDetails: false,
            cancellationToken: cancellationToken);

        var existingTagIds = existing.Select(x => x.TagId).Distinct().ToList();

        var toAdd = targetIds.Except(existingTagIds).ToList();
        var toRemove = existingTagIds.Except(targetIds).ToList();

        // 2) 删除不再需要的关联
        if (toRemove.Count > 0)
        {
            var removed = existing.Where(x => toRemove.Contains(x.TagId)).ToList();
            foreach (var rel in removed)
            {
                await _documentTagRepository.DeleteAsync(rel, autoSave: false, cancellationToken);
            }
        }

        // 3) 添加新的关联
        if (toAdd.Count > 0)
        {
            var now = Clock.Now;

            foreach (var tid in toAdd)
            {
                var rel = new DocumentTag(
                    documentId: doc.Id,
                    tagId: tid,
                    bindTime: now);

                await _documentTagRepository.InsertAsync(rel, autoSave: false, cancellationToken);
            }
        }

        // 注意：这里不调用 autoSave=true，交给外层 UoW 一次性提交更安全更快；
        // 如果你当前调用链没有 UoW，也可以在上层 AppService 包裹 UnitOfWork。
    }
}
