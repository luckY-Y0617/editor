using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Shared.Enums;

namespace NS.Module.Knowledge.Domain.KnowledgeBases;

public interface IKnowledgeBaseRepository : ISqlSugarRepository<KnowledgeBase, Guid>
{
    /// <summary>
    /// 通过唯一编码查找知识库（如果你启用了 Code 字段）。
    /// </summary>
    Task<KnowledgeBase?> FindByCodeAsync(
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取当前租户下的知识库列表，支持简单搜索。
    /// </summary>
    Task<List<KnowledgeBase>> GetListAsync(
        Guid? tenantId,
        string? filter = null,
        KnowledgeBaseVisibility? visibility = null,
        CancellationToken cancellationToken = default);
}
