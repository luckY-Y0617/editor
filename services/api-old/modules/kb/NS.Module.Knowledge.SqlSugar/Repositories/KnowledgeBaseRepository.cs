using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.KnowledgeBases;
using NS.Module.Knowledge.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories
{
    /// <summary>
    /// 知识库仓储实现。
    /// 注意：请将 KnowledgeSqlSugarDbContext 替换为你的实际 DbContext 类型。
    /// </summary>
    public class KnowledgeBaseRepository
        : SqlSugarRepository<KnowledgeSqlSugarDbContext, KnowledgeBase, Guid>,
          IKnowledgeBaseRepository, 
          ITransientDependency
    {
        public KnowledgeBaseRepository(
            ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> provider)
            : base(provider)
        {
        }

        /// <summary>
        /// 通过唯一编码查找知识库。
        /// </summary>
        public async Task<KnowledgeBase?> FindByCodeAsync(
            string code,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return null;
            }

            var query = await GetSugarQueryableAsync();

            return await query
                .FirstAsync(kb => kb.Code == code && !kb.IsDeleted, cancellationToken);
        }

        /// <summary>
        /// 获取当前租户下的知识库列表
        /// </summary>
        public async Task<List<KnowledgeBase>> GetListAsync(
            Guid? tenantId,
            string? filter = null,
            KnowledgeBaseVisibility? visibility = null,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            if (tenantId.HasValue)
            {
                var tid = tenantId.Value;
                query = query.Where(kb => kb.TenantId == tid);
            }

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var keyword = filter.Trim();
                query = query.Where(kb => kb.Name.Contains(keyword)
                                          || kb.Description!.Contains(keyword));
            }

            if (visibility.HasValue)
            {
                var vis = visibility.Value;
                query = query.Where(kb => kb.Visibility == vis);
            }

            return await query
                .Where(kb => !kb.IsDeleted)
                .OrderBy(kb => kb.SortOrder)
                .OrderBy(kb => kb.CreationTime, OrderByType.Desc)
                .ToListAsync(cancellationToken);
        }
    }
}
