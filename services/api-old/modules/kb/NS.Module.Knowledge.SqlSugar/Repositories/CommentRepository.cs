using NS.Framework.SqlSugar.Abstractions;
using NS.Module.Knowledge.Domain.Comments;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Knowledge.SqlSugar.Repositories;

public class CommentRepository
    : SqlSugarRepository<KnowledgeSqlSugarDbContext, Comment, Guid>,
    ICommentRepository,
    ITransientDependency
{
    public CommentRepository(ISqlSugarDbContextProvider<KnowledgeSqlSugarDbContext> dbContextProvider)
        : base(dbContextProvider)
    {
    }

    public virtual async Task<List<Comment>> GetByDocumentIdAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // 注意：FullAuditedAggregateRoot 有 IsDeleted
        var query = await GetSugarQueryableAsync();

        return await query
            .Where(c => c.DocumentId == documentId && !c.IsDeleted)
            .OrderBy(c => c.CreationTime) // 平铺返回更利于应用层组树
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<List<Comment>> GetByIdsAsync(
        IEnumerable<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var idList = ids.Distinct().ToList() ?? new List<Guid>();
        if (idList.Count == 0) return new List<Comment>();

        var query = await GetSugarQueryableAsync();

        return await query
            .Where(c => idList.Contains(c.Id) && !c.IsDeleted)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<bool> IsInDocumentAsync(
        Guid commentId,
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = await GetSugarQueryableAsync();

        return await query
            .Where(c => c.Id == commentId && c.DocumentId == documentId && !c.IsDeleted)
            .AnyAsync(cancellationToken);
    }
}