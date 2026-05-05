using System;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Shared.Enums;
using NS.Module.Knowledge.Domain.Comments;
using NS.Module.Knowledge.Domain.Versions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.Documents;

/// <summary>
/// 文档内容管理器：
/// - 维护当前文档内容（Upsert）
/// - 统一处理评论锚点重定位（唯一真相源：CommentAnchorResolver）
/// - 负责文档版本生成规则
/// 不负责 Document 聚合的创建 / 结构变更。
/// </summary>
public class DocumentContentManager : DomainService
{
    private readonly ISqlSugarRepository<DocumentContent, Guid> _contentRepository;
    private readonly IDocumentVersionRepository _versionRepository;
    private readonly IDocumentRepository _documentRepository;

    private readonly ICommentRepository _commentRepository;
    private readonly CommentManager _commentManager;

    public DocumentContentManager(
        ISqlSugarRepository<DocumentContent, Guid> contentRepository,
        IDocumentVersionRepository versionRepository,
        IDocumentRepository documentRepository,
        ICommentRepository commentRepository,
        CommentManager commentManager)
    {
        _contentRepository = contentRepository;
        _versionRepository = versionRepository;
        _documentRepository = documentRepository;
        _commentRepository = commentRepository;
        _commentManager = commentManager;
    }

    #region Initial Content

    /// <summary>
    /// 创建初始内容。
    /// 只能在 Document 创建完成后调用。
    /// </summary>
    public virtual async Task CreateInitialContentAsync(
        Document doc,
        string? initialContentJson,
        CancellationToken cancellationToken = default)
    {
        var contentJson = initialContentJson ?? string.Empty;

        await SaveContentAsync(
            doc,
            contentJson: contentJson,
            contentHtml: null,
            plainText: null,
            versionSource: DocumentVersionSource.Initial,
            changeSummary: "初始内容",
            cancellationToken);
    }

    #endregion

    #region Save Content

    /// <summary>
    /// 保存文档内容：
    /// - Upsert 当前内容
    /// - 统一处理评论锚点重定位（唯一真相源：CommentAnchorResolver）
    /// - 根据 VersionSource 决定是否生成版本
    /// </summary>
public virtual async Task SaveContentAsync(
    Document doc,
    string contentJson,
    string? contentHtml,
    string? plainText,
    DocumentVersionSource versionSource,
    string? changeSummary,
    CancellationToken cancellationToken = default)
{
    cancellationToken.ThrowIfCancellationRequested();

    // 1️⃣ 读取当前内容（用于判断是否首次创建）
    var existing = await _contentRepository.FindAsync(
        doc.Id,
        includeDetails: false,
        cancellationToken);

    var isFirstSave = existing == null;

    // 2️⃣ Upsert 当前内容
    if (existing == null)
    {
        var newContent = new DocumentContent(
            documentId: doc.Id,
            contentJson: contentJson,
            contentHtml: contentHtml,
            plainText: plainText);

        await _contentRepository.InsertAsync(
            newContent,
            autoSave: true,
            cancellationToken);
    }
    else
    {
        existing.Update(contentJson, contentHtml, plainText);

        await _contentRepository.UpdateAsync(
            existing,
            autoSave: true,
            cancellationToken);
    }

    // 3️⃣ 更新文档内容更新时间（用于列表 / 排序 / 协同）
    doc.SetProperty(nameof(Document.LastContentUpdateTime), Clock.Now);
    await _documentRepository.UpdateAsync(doc, autoSave: true, cancellationToken);

    // 4️⃣ 评论锚点重定位：移除
    // 说明：
    // - 锚点是否可定位由前端 locateRangeAnchor() 决定
    // - 后端不解析 positionJson，不维护 CommentStatus/ResolveVersionId 等字段
    // - 文本删除导致 locate 失败：前端不高亮 + UI 显示“原文已删除/不存在”

    // 5️⃣ 是否生成版本（Initial / ManualSave / Restore）
    if (!ShouldCreateVersion(versionSource))
    {
        return;
    }

    var nextVersionNumber =
        await _versionRepository.GetNextVersionNumberAsync(
            doc.Id,
            cancellationToken);

    var version = new DocumentVersion(
        documentId: doc.Id,
        versionNumber: nextVersionNumber,
        snapshotJson: contentJson,
        snapshotHtml: contentHtml,
        snapshotPlainText: plainText,
        source: versionSource,
        changeSummary: NormalizeChangeSummary(versionSource, changeSummary));

    await _versionRepository.InsertAsync(
        version,
        autoSave: true,
        cancellationToken);
}


    #endregion

    #region Restore

    /// <summary>
    /// 从指定历史版本恢复：
    /// - 覆盖当前内容
    /// - 生成一个新的 Restore 版本
    /// </summary>
    public virtual async Task RestoreFromVersionAsync(
        Document doc,
        DocumentVersion version,
        CancellationToken cancellationToken = default)
    {
        await SaveContentAsync(
            doc,
            contentJson: version.SnapshotJson,
            contentHtml: version.SnapshotHtml,
            plainText: version.SnapshotPlainText,
            versionSource: DocumentVersionSource.Restore,
            changeSummary: $"从版本 v{version.VersionNumber} 恢复",
            cancellationToken);
    }

    #endregion

    #region Helpers

    private static bool ShouldCreateVersion(DocumentVersionSource source)
    {
        return source switch
        {
            DocumentVersionSource.Initial    => true,
            DocumentVersionSource.ManualSave => true,
            DocumentVersionSource.Restore    => true,
            _ => false // AutoSave
        };
    }

    private static string? NormalizeChangeSummary(
        DocumentVersionSource source,
        string? originalSummary)
    {
        if (!originalSummary.IsNullOrWhiteSpace())
        {
            return originalSummary;
        }

        return source switch
        {
            DocumentVersionSource.Initial    => "初始内容",
            DocumentVersionSource.ManualSave => "手动保存",
            DocumentVersionSource.Restore    => "从历史版本恢复",
            _ => null
        };
    }

    #endregion
}
