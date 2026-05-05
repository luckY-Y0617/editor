using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Knowledge.Domain.Tags;

public interface ITagRepository : ISqlSugarRepository<Tag, Guid>
{
    /// <summary>
    /// 获取某知识库下的全部标签，可选名称过滤。
    /// </summary>
    Task<List<Tag>> GetListByKnowledgeBaseIdAsync(
        Guid knowledgeBaseId,
        string? filter = null,
        CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取使用次数最多的标签（用于热门标签等）。
    /// </summary>
    Task<List<Tag>> GetTopTagsAsync(
        Guid knowledgeBaseId,
        int maxCount = 20,
        CancellationToken cancellationToken = default);
}
