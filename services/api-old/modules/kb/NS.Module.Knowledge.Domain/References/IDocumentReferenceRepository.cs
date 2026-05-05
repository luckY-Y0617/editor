using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.References;

namespace NS.Module.Knowledge.Domain.References;

public interface IDocumentReferenceRepository : ISqlSugarRepository<DocumentReference, Guid>
{
    /// <summary>
    /// 获取某文档引用出去的所有引用（Outgoing）。
    /// </summary>
    Task<List<DocumentReference>> GetOutgoingAsync(
        Guid sourceDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取某文档被哪些文档引用（Incoming）。
    /// </summary>
    Task<List<DocumentReference>> GetIncomingAsync(
        Guid targetDocumentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除某文档所有“作为源”的引用记录（一般在保存内容前清空，再重建）。
    /// </summary>
    Task DeleteBySourceAsync(
        Guid sourceDocumentId,
        CancellationToken cancellationToken = default);
}
