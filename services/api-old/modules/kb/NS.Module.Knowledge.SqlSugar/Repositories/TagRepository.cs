using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Tags;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories
{
    /// <summary>
    /// 标签仓储实现。
    /// </summary>
    public class TagRepository
        : SqlSugarRepository<KnowledgeSqlSugarDbContext, Tag, Guid>,
          ITagRepository, ITransientDependency
    {
        public TagRepository(
            ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> provider)
            : base(provider)
        {
        }

        /// <summary>
        /// 获取某知识库下的全部标签，可选名称过滤。
        /// </summary>
        public async Task<List<Tag>> GetListByKnowledgeBaseIdAsync(
            Guid knowledgeBaseId,
            string? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            query = query.Where(t => t.KnowledgeBaseId == knowledgeBaseId && !t.IsDeleted);

            if (!string.IsNullOrWhiteSpace(filter))
            {
                var keyword = filter.Trim();
                query = query.Where(t => t.Name.Contains(keyword));
            }

            // 简单排序：使用次数 desc，其次按 Name
            return await query
                .OrderBy(t => t.Name)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 在同一个知识库下，通过标签名查找（用于防重）。
        /// </summary>
        public async Task<Tag?> FindByNameAsync(
            Guid knowledgeBaseId,
            string name,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            var query = await GetSugarQueryableAsync();
            var trimName = name.Trim();

            return await query
                .FirstAsync(t =>
                        t.KnowledgeBaseId == knowledgeBaseId
                        && t.Name == trimName
                        && !t.IsDeleted,
                    cancellationToken);
        }

        /// <summary>
        /// 获取使用次数最多的标签（热门标签）。
        /// </summary>
        public async Task<List<Tag>> GetTopTagsAsync(
            Guid knowledgeBaseId,
            int maxCount = 20,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            return await query
                .Where(t => t.KnowledgeBaseId == knowledgeBaseId && !t.IsDeleted)
                .Take(maxCount)
                .ToListAsync(cancellationToken);
        }
    }
}
