using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Knowledge.Domain.Documents;

/// <summary>
/// 这里没有返回 IQueryable，全部是 Task + List，方便你后面用 SqlSugar 自己实现查询。
///GetWithContentAsync 的内容本质会通过 DocumentContentRepository 或 SqlSugar 导航加载。
/// </summary>
public interface IDocumentRepository : ISqlSugarRepository<Document, Guid>
{
    Task<int> GetCommentCountAsync(Guid documentId);
    Task<int> GetVersionCountAsync(Guid documentId);

    
    /// <summary>
    /// 获取某个知识库下的整棵文档树（一般用于 Overview / 编辑器左侧树）。
    /// </summary>
    Task<List<Document>> GetTreeAsync(
        Guid knowledgeBaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取某个父节点下的所有子文档（不含孙子），按 Order 排序。
    /// parentId 为 null 时返回根节点列表。
    /// </summary>
    Task<List<Document>> GetChildrenAsync(
        Guid knowledgeBaseId,
        Guid? parentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取文档的“路径”（面包屑）：从根到当前节点的链路。
    /// </summary>
    Task<List<Document>> GetBreadcrumbAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取同一个父节点下当前最大的排序值，用于插入新文档时确定 Order。
    /// </summary>
    Task<int> GetNextOrderAsync(
        Guid knowledgeBaseId,
        Guid? parentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取包含最新内容的文档（常用于文档详情页）。
    /// 实现层通常会 Internally Join DocumentContent。
    /// </summary>
    Task<Document?> GetWithContentAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

}
