using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Knowledge.Domain.Versions;

public interface IDocumentVersionRepository : ISqlSugarRepository<DocumentVersion, Guid>
{
    /// <summary>
    /// 获取某文档的所有版本（按时间倒序）。
    /// </summary>
    Task<List<DocumentVersion>> GetListByDocumentIdAsync(
        Guid documentId,
        int? maxCount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取某文档最近一次版本记录。
    /// </summary>
    Task<DocumentVersion?> GetLastVersionAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);
    
    Task<int> GetNextVersionNumberAsync(
        Guid documentId, 
        CancellationToken cancellationToken = default);

}
