using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Knowledge.Domain.Shared.Enums;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.Documents;

public class DocumentManager : DomainService
{
    private readonly IDocumentRepository _documentRepository;
    private readonly DocumentContentManager _documentContentManager;

    public DocumentManager(
        IDocumentRepository documentRepository,
        DocumentContentManager documentContentManager)
    {
        _documentRepository = documentRepository;
        _documentContentManager = documentContentManager;
    }

    public virtual async Task<Document> CreateAsync(
        Guid knowledgeBaseId,
        string title,
        Guid? parentId = null,
        DocumentType type = DocumentType.Normal,
        string? initialContentJson = null,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrWhiteSpace(title, nameof(title));

        // 验证父节点：文档(type=0)不能作为其他文档的父节点，只能在文件夹(type=1)或根目录下创建
        if (parentId.HasValue)
        {
            var parentDoc = await _documentRepository.GetAsync(parentId.Value, cancellationToken: cancellationToken);
            if (parentDoc != null && !parentDoc.IsDeleted && parentDoc.Type != DocumentType.Folder)
            {
                throw new BusinessException("Document:CannotCreateUnderDocument")
                    .WithData("Message", "文档下面不能创建文档，只能在文件夹或根目录下创建");
            }
        }

        var order = await _documentRepository.GetNextOrderAsync(
            knowledgeBaseId,
            parentId,
            cancellationToken);

        var doc = new Document(
            knowledgeBaseId: knowledgeBaseId,
            title: title,
            parentId: parentId);

        // 设置文档类型
        doc.SetType(type);

        doc.MoveTo(parentId, order);

        await _documentRepository.InsertAsync(doc, autoSave: true, cancellationToken);

        // 只有普通文档才创建内容
        if (type == DocumentType.Normal)
        {
            await _documentContentManager.CreateInitialContentAsync(
                doc,
                initialContentJson,
                cancellationToken);
        }

        return doc;
    }

    public virtual async Task RenameAsync(
        Document doc,
        string newTitle,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrWhiteSpace(newTitle, nameof(newTitle));

        doc.Rename(newTitle);
        await _documentRepository.UpdateAsync(doc, autoSave: true, cancellationToken);
    }

    /// <summary>
    /// 移动节点：更新 ParentId + Order。
    /// 防御：不能移动到自己或自己的子树下面。
    /// </summary>
    public virtual async Task MoveAsync(
        Document doc,
        Guid? newParentId,
        int newOrder,
        CancellationToken cancellationToken = default)
    {
        if (newParentId.HasValue && newParentId.Value == doc.Id)
        {
            throw new BusinessException("Document:CannotMoveToSelf");
        }

        if (newParentId.HasValue)
        {
            var kbId = doc.KnowledgeBaseId;

            var allDocs = await _documentRepository.GetTreeAsync(kbId, cancellationToken);
            var allMap = allDocs.ToDictionary(d => d.Id, d => d);

            if (IsDescendant(doc.Id, newParentId.Value, allMap))
            {
                throw new BusinessException("Document:CannotMoveToOwnDescendant");
            }
        }

        doc.MoveTo(newParentId, newOrder);
        await _documentRepository.UpdateAsync(doc, autoSave: true, cancellationToken);
    }

    private static bool IsDescendant(
        Guid rootId,
        Guid targetId,
        IDictionary<Guid, Document> docs)
    {
        var queue = new Queue<Guid>();
        queue.Enqueue(rootId);

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var child in docs.Values.Where(d => d.ParentId == currentId))
            {
                if (child.Id == targetId)
                {
                    return true;
                }

                queue.Enqueue(child.Id);
            }
        }

        return false;
    }

    /// <summary>
    /// 删除文档。
    /// includeChildren=true 时递归删除整个子树。
    /// </summary>
    public virtual async Task DeleteAsync(
        Document doc,
        bool includeChildren = true,
        CancellationToken cancellationToken = default)
    {
        if (!includeChildren)
        {
            await _documentRepository.DeleteAsync(doc, autoSave: true, cancellationToken);
            return;
        }

        var kbId = doc.KnowledgeBaseId;
        var allDocs = await _documentRepository.GetTreeAsync(kbId, cancellationToken);
        var toDelete = GetSubTree(doc.Id, allDocs);

        foreach (var d in toDelete)
        {
            await _documentRepository.DeleteAsync(d, autoSave: false, cancellationToken);
        }
    }

    private static List<Document> GetSubTree(Guid rootId, List<Document> all)
    {
        var result = new List<Document>();
        var map = all.ToLookup(d => d.ParentId);

        void Dfs(Guid id)
        {
            var node = all.FirstOrDefault(d => d.Id == id);
            if (node == null)
            {
                return;
            }

            result.Add(node);

            foreach (var child in map[id])
            {
                Dfs(child.Id);
            }
        }

        Dfs(rootId);
        return result;
    }
}
