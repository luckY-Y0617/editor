using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.References;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories
{
    /// <summary>
    /// 文档引用关系仓储实现：
    /// - DocumentReference 用于描述文档之间的引用关系
    /// - 提供按源文档 / 目标文档查询 & 按源文档批量删除
    /// </summary>
    public class DocumentReferenceRepository
        : SqlSugarRepository<KnowledgeSqlSugarDbContext, DocumentReference, Guid>,
          IDocumentReferenceRepository,
          ITransientDependency
    {
        public DocumentReferenceRepository(
            ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> dbContextProvider)
            : base(dbContextProvider)
        {
        }

        /// <summary>
        /// 获取某文档引用出去的所有引用（Outgoing）。
        /// </summary>
        public async Task<List<DocumentReference>> GetOutgoingAsync(
            Guid sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            var db = await GetDbContextAsync();

            return await db.Client.Queryable<DocumentReference>()
                .Where(x => x.SourceDocumentId == sourceDocumentId)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 获取某文档被哪些文档引用（Incoming）。
        /// </summary>
        public async Task<List<DocumentReference>> GetIncomingAsync(
            Guid targetDocumentId,
            CancellationToken cancellationToken = default)
        {
            var db = await GetDbContextAsync();

            return await db.Client.Queryable<DocumentReference>()
                .Where(x => x.TargetDocumentId == targetDocumentId)
                .ToListAsync(cancellationToken);
        }

        /// <summary>
        /// 删除某文档所有“作为源”的引用记录（一般在保存内容前清空，再重建）。
        /// </summary>
        public async Task DeleteBySourceAsync(
            Guid sourceDocumentId,
            CancellationToken cancellationToken = default)
        {
            var db = await GetDbContextAsync();

            await db.Client.Deleteable<DocumentReference>()
                .Where(x => x.SourceDocumentId == sourceDocumentId)
                .ExecuteCommandAsync(cancellationToken);
        }
    }
}
