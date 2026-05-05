using System.Linq.Expressions;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace NS.Framework.SqlSugar.Abstractions
{
    public interface ISqlSugarRepository<TEntity> : IRepository<TEntity>
        where TEntity : class, IEntity, new()
    {
        Task<ISqlSugarDbContext> GetDbContextAsync();

        new Task<ISugarQueryable<TEntity>> GetQueryableAsync();

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate);
    }

    public interface ISqlSugarRepository<TEntity, TKey> :
        ISqlSugarRepository<TEntity>,
        IRepository<TEntity, TKey>
        where TEntity : class, IEntity<TKey>, new()
    {
    }
}