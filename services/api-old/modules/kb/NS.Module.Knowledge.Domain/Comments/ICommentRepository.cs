using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Knowledge.Domain.Comments;

public interface ICommentRepository : ISqlSugarRepository<Comment, Guid>
{
    /// <summary>
    /// 获取某文档下所有评论（包含回复，平铺返回；由应用层自行组树）。
    /// 默认排 CreationTime 升序，方便组树后回复顺序稳定。
    /// </summary>
    Task<List<Comment>> GetByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量获取（用于权限校验/删除等场景）。
    /// </summary>
    Task<List<Comment>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 判断某评论是否属于指定文档（用于防止跨文档操作）。
    /// </summary>
    Task<bool> IsInDocumentAsync(
        Guid commentId,
        Guid documentId,
        CancellationToken cancellationToken = default);
}