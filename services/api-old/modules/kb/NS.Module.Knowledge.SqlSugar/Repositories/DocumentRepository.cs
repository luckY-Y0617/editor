using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Comments;
using NS.Module.Knowledge.Domain.Documents;
using NS.Module.Knowledge.Domain.Versions;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories
{
    /// <summary>
    /// 文档仓储实现：负责文档树、面包屑、排序等查询。
    /// 注意：这里的 TDbContext 请替换成你实际的 DbContext 类型，比如 KnowledgeSqlSugarDbContext。
    /// </summary>
    public class DocumentRepository
        : SqlSugarRepository<KnowledgeSqlSugarDbContext, Document, Guid>, IDocumentRepository, ITransientDependency
        // ↑ 这里的 KnowledgeSqlSugarDbContext 用你的实际 DbContext 替换
    {
        public DocumentRepository(ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> provider)
            : base(provider)
        {
        }
        
        public async Task<int> GetCommentCountAsync(Guid documentId)
        {
            var dbcontext = await GetDbContextAsync();
            
            return await dbcontext.Client.Queryable<Comment>()
                .Where(x => x.DocumentId == documentId && !x.IsDeleted)
                .CountAsync();
        }

        public async Task<int> GetVersionCountAsync(Guid documentId)
        {
            var dbcontext = await GetDbContextAsync();

            
            return await dbcontext.Client.Queryable<DocumentVersion>()
                .Where(x => x.DocumentId == documentId)
                .CountAsync();
        }

        /// <summary>
        /// 获取某个知识库下的整棵文档树（实际上是该 KB 下所有文档的扁平列表，按 ParentId + Order 排好）。
        /// 真正组装成树结构可以放在 ApplicationService 里做。
        /// </summary>
        public async Task<List<Document>> GetTreeAsync(
            Guid knowledgeBaseId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            return await query
                .Where(d => d.KnowledgeBaseId == knowledgeBaseId && !d.IsDeleted)
                .OrderBy(d => d.ParentId)
                .OrderBy(d => d.Order)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取某父节点下的直接子文档（不含孙子），按 Order 排序。
        /// parentId == null 表示根节点。
        /// </summary>
        public async Task<List<Document>> GetChildrenAsync(
            Guid knowledgeBaseId,
            Guid? parentId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            query = query.Where(d => d.KnowledgeBaseId == knowledgeBaseId && !d.IsDeleted);

            if (parentId == null)
            {
                query = query.Where(d => d.ParentId == null);
            }
            else
            {
                var pid = parentId.Value;
                query = query.Where(d => d.ParentId == pid);
            }

            return await query
                .OrderBy(d => d.Order)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取文档的“面包屑”：从根节点到当前文档的路径。
        /// </summary>
        public async Task<List<Document>> GetBreadcrumbAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            var result = new List<Document>();

            var dbContext = await GetDbContextAsync();
            var client = dbContext.Client;

            // 先拿当前文档
            var current = await client.Queryable<Document>()
                .FirstAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);

            if (current == null)
            {
                return result;
            }

            result.Add(current);

            // 逐级向上找 parent
            while (current.ParentId.HasValue)
            {
                var pid = current.ParentId.Value;

                current = await client.Queryable<Document>()
                    .FirstAsync(d => d.Id == pid && !d.IsDeleted, cancellationToken);

                if (current == null)
                {
                    break;
                }

                result.Add(current);
            }

            // 当前在最前，反转一下：根 → ... → 当前
            result.Reverse();
            return result;
        }

        /// <summary>
        /// 获取同一 Parent 下当前最大的排序值，用于新建文档时确定下一个 Order。
        /// </summary>
        public async Task<int> GetNextOrderAsync(
            Guid knowledgeBaseId,
            Guid? parentId,
            CancellationToken cancellationToken = default)
        {
            var query = await GetSugarQueryableAsync();

            query = query.Where(d => d.KnowledgeBaseId == knowledgeBaseId && !d.IsDeleted);

            if (parentId == null)
            {
                query = query.Where(d => d.ParentId == null);
            }
            else
            {
                var pid = parentId.Value;
                query = query.Where(d => d.ParentId == pid);
            }

            var maxOrder = await query.MaxAsync(d => d.Order, cancellationToken);
            return maxOrder + 1;
        }

        /// <summary>
        /// 获取包含最新内容的文档。这里默认走 WithDetails（前提是你在 SqlSugarEntityOptions 里配置了导航）。
        /// 如果你暂时没有配置导航，可以改成手动 join DocumentContent。
        /// </summary>
        public async Task<Document?> GetWithContentAsync(
            Guid documentId,
            CancellationToken cancellationToken = default)
        {
            // 推荐：在 SqlSugarEntityOptions<Document> 的 DefaultWithDetailsFunc 中配置 Include Content/Tags 等
            var query = await WithDetailsAsync();

            return await query
                .FirstAsync(d => d.Id == documentId && !d.IsDeleted, cancellationToken);
        }
    }
}
