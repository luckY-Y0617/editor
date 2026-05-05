using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Module.Knowledge.Domain.Members;

public interface IKnowledgeBaseMemberRepository
    : ISqlSugarRepository<KnowledgeBaseMember, Guid>
{
    Task<KnowledgeBaseMember?> FindByBaseAndUserAsync(
        Guid knowledgeBaseId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<List<KnowledgeBaseMember>> GetListByBaseAsync(
        Guid knowledgeBaseId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询用户在哪些（给定的） KnowledgeBase 中是成员。
    /// 用于一次性批量判断，避免 N+1 查询。
    /// </summary>
    Task<List<Guid>> GetKbIdsByUserAsync(
        Guid userId,
        IEnumerable<Guid> knowledgeBaseIds,
        CancellationToken cancellationToken = default);
}
