using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Versions;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories
{
    /// <summary>
    /// 文档版本仓储实现。
    /// </summary>
    public class DocumentVersionRepository
        : SqlSugarRepository<KnowledgeSqlSugarDbContext, DocumentVersion, Guid>,
          IDocumentVersionRepository,
          ITransientDependency
    {
        public DocumentVersionRepository(
            ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> provider)
            : base(provider)
        {
        }

        /// <summary>
        /// 获取某文档的所有版本（按版本号倒序），可选限制数量。
        /// </summary>
        public async Task<List<DocumentVersion>> GetListByDocumentIdAsync(
            Guid documentId,
            int? maxCount = null,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            query = query.Where(v => v.DocumentId == documentId);

            // ❗这里用 VersionNumber 而不是 CreationTime
            query = query.OrderBy(v => v.VersionNumber, OrderByType.Desc);

            if (maxCount.HasValue && maxCount.Value > 0)
            {
                query = query.Take(maxCount.Value);
            }

            return await query.ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取某文档最近一次版本记录（按版本号倒序）。
        /// </summary>
        public async Task<DocumentVersion?> GetLastVersionAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            return await query
                .Where(v => v.DocumentId == documentId)
                .OrderBy(v => v.VersionNumber, OrderByType.Desc)
                .FirstAsync(cancellationToken);
        }

        /// <summary>
        /// 获取某文档的下一个版本号（当前最大版本号 + 1）。
        /// 若暂无任何版本，则返回 1。
        /// </summary>
        public async Task<int> GetNextVersionNumberAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            // Max 可能为空，因此用 int? 接，然后 ?? 0
            var maxNumber = await query
                .Where(v => v.DocumentId == documentId)
                .MaxAsync(v => (int?)v.VersionNumber);

            return (maxNumber ?? 0) + 1;
        }
    }
}
