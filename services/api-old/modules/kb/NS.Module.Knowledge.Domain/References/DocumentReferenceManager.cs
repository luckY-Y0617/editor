using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.References;

public class DocumentReferenceManager : DomainService
{
    private readonly IDocumentReferenceRepository _referenceRepository;

    public DocumentReferenceManager(IDocumentReferenceRepository referenceRepository)
    {
        _referenceRepository = referenceRepository;
    }

    /// <summary>
    /// 替换某文档的全部引用关系：
    /// 调用前应用层需要先解析 DocumentContent JSON 得出目标列表。
    /// </summary>
    public virtual async Task ReplaceReferencesAsync(
        Guid sourceDocumentId,
        IEnumerable<(Guid targetId, DocumentReferenceType type, string? excerpt)> references,
        CancellationToken cancellationToken = default)
    {
        var now = Clock.Now;

        // 1. 先清空旧的引用记录
        await _referenceRepository.DeleteBySourceAsync(sourceDocumentId, cancellationToken);

        // 2. 插入新的记录
        foreach (var (targetId, type, excerpt) in references)
        {
            var reference = new DocumentReference(
                sourceDocumentId: sourceDocumentId,
                targetDocumentId: targetId,
                type: type,
                now: now,
                excerpt: excerpt);

            await _referenceRepository.InsertAsync(reference, autoSave: false, cancellationToken);
        }
    }
}

