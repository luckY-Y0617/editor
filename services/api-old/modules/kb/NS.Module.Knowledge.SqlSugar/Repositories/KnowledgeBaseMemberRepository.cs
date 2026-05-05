using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Members;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories
{
    public class KnowledgeBaseMemberRepository
        : SqlSugarRepository<KnowledgeSqlSugarDbContext, KnowledgeBaseMember, Guid>,
            IKnowledgeBaseMemberRepository, 
            ITransientDependency
    {
        public KnowledgeBaseMemberRepository(
            ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> provider)
            : base(provider)
        {
        }

        public async Task<KnowledgeBaseMember?> FindByBaseAndUserAsync(
            Guid knowledgeBaseId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            return await query.FirstAsync(
                m => m.KnowledgeBaseId == knowledgeBaseId
                     && m.UserId == userId,
                cancellationToken);
        }

        public async Task<List<KnowledgeBaseMember>> GetListByBaseAsync(
            Guid knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            return await query
                .Where(m => m.KnowledgeBaseId == knowledgeBaseId)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Guid>> GetKbIdsByUserAsync(Guid userId, IEnumerable<Guid> knowledgeBaseIds, CancellationToken cancellationToken = default)
        {
            // 自然空判断处理
            var kbIdList = knowledgeBaseIds.ToList();
            if (kbIdList.Count == 0) 
                return [];

            var query = await GetSugarQueryableAsync();

            return await query
                .Where(m => m.UserId == userId && kbIdList.Contains(m.KnowledgeBaseId))
                .Select(m => m.KnowledgeBaseId)
                .ToListAsync(cancellationToken);
        }
    }
}