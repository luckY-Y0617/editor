using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.SqlSugar;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NS.Framework.SqlSugar.Abstractions;

namespace NS.Framework.SqlSugar
{
    public class SqlSugarRepository<TDbContext, TEntity> : RepositoryBase<TEntity>, ISqlSugarRepository<TEntity>
        where TDbContext : class, ISqlSugarDbContext
        where TEntity : class, IEntity, new()
    {
        private readonly ISqlSugarDbContextProvider<TDbContext> _contextProvider;
        private readonly Lazy<SqlSugarEntityOptions<TEntity>> _entityOptions;

        protected virtual SqlSugarEntityOptions<TEntity> EntityOptions => _entityOptions.Value;

        public SqlSugarRepository(ISqlSugarDbContextProvider<TDbContext> contextProvider) : base(nameof(contextProvider))
        {
            _contextProvider = contextProvider;
            _entityOptions = new Lazy<SqlSugarEntityOptions<TEntity>>(
                () => ServiceProvider
                    .GetRequiredService<IOptions<SqlSugarEntityOptions>>()
                    .Value
                    .GetOrNull<TEntity>() ?? SqlSugarEntityOptions<TEntity>.Empty);
        }

        async Task<ISqlSugarDbContext> ISqlSugarRepository<TEntity>.GetDbContextAsync()
        {
            return await GetDbContextAsync();
        }

        protected virtual Task<TDbContext> GetDbContextAsync()
        {
            return _contextProvider.GetDbContextAsync();
        }

        Task<ISugarQueryable<TEntity>> ISqlSugarRepository<TEntity>.GetQueryableAsync()
        {
            return GetSugarQueryableAsync();
        }


        [Obsolete("Use GetQueryableAsync method.")]
        protected override IQueryable<TEntity> GetQueryable()
            => throw new NotSupportedException("同步方法已弃用");

        [Obsolete("SqlSugar 不支持 IQueryable<TEntity>，请使用 ISqlSugarRepository<TEntity>.GetQueryableAsync()")]
        public override Task<IQueryable<TEntity>> GetQueryableAsync()
        {
            throw new NotSupportedException(
                "SqlSugar 不支持 IQueryable<TEntity>，请使用 ISqlSugarRepository<TEntity>.GetQueryableAsync()");
        }

        protected virtual async Task<ISugarQueryable<TEntity>> GetSugarQueryableAsync()
        {
            var db = await GetDbContextAsync();
            return db.Client.Queryable<TEntity>();
        }


        public override async Task<TEntity> InsertAsync(
            TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var db = await GetDbContextAsync();
            await db.Client.Insertable(entity).ExecuteCommandAsync(cancellationToken);
            return entity;
        }

        public override async Task<TEntity> UpdateAsync(
            TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var db = await GetDbContextAsync();

            try
            {
                // 支持乐观锁更新
                var rows = await db.Client.Updateable(entity)
                    .ExecuteCommandWithOptLockAsync(true);

                if (rows == 0)
                    throw new AbpDbConcurrencyException("并发更新失败：数据可能已被修改。");
            }
            catch (AbpDbConcurrencyException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new AbpException($"更新实体 {typeof(TEntity).Name} 失败: {ex.Message}", ex);
            }

            return entity;
        }

        public override async Task DeleteAsync(
            TEntity entity, bool autoSave = false, CancellationToken cancellationToken = default)
        {
            var db = await GetDbContextAsync();
            await db.Client.Deleteable(entity).ExecuteCommandAsync(cancellationToken);
        }
        
        public override async Task<long> GetCountAsync(CancellationToken cancellationToken = default)
        {
            return await (await GetSugarQueryableAsync()).CountAsync(cancellationToken);
        }

        public override async Task<List<TEntity>> GetListAsync(
            Expression<Func<TEntity, bool>> predicate,
            bool includeDetails = false,
            CancellationToken cancellationToken = default)
        {
            var query = includeDetails
                ? await WithDetailsAsync()
                : await GetSugarQueryableAsync();

            return await query.Where(predicate).ToListAsync(cancellationToken);
        }

        public override async Task<List<TEntity>> GetListAsync(
            bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            return includeDetails
                ? await (await WithDetailsAsync()).ToListAsync(cancellationToken)
                : await (await GetSugarQueryableAsync()).ToListAsync(cancellationToken);
        }

        public override async Task<List<TEntity>> GetPagedListAsync(
            int skipCount, int maxResultCount, string sorting,
            bool includeDetails = false, CancellationToken cancellationToken = default)
        {
            var query = includeDetails
                ? await WithDetailsAsync()
                : await GetSugarQueryableAsync();

            int pageNumber = (skipCount / maxResultCount) + 1;
            RefAsync<int> totalNumber = 0;
            return await query
                .OrderByIF(!sorting.IsNullOrWhiteSpace(), sorting)
                .ToPageListAsync(pageNumber, maxResultCount, totalNumber, cancellationToken);
        }

        public override async Task<TEntity?> FindAsync(
            Expression<Func<TEntity, bool>> predicate, bool includeDetails = true,
            CancellationToken cancellationToken = default)
        {
            return includeDetails
                ? await (await WithDetailsAsync()).SingleAsync(predicate)
                : await (await GetSugarQueryableAsync()).SingleAsync(predicate);
        }

        public override async Task DeleteAsync(Expression<Func<TEntity, bool>> predicate, bool autoSave = false,
            CancellationToken cancellationToken = new CancellationToken())
        {
            var dbContext = await GetDbContextAsync();
            await dbContext.Client.Deleteable<TEntity>().Where(predicate).ExecuteCommandAsync(cancellationToken);
        }
        
        
        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>> predicate)
        {
            var query = await GetSugarQueryableAsync();
            return await query.AnyAsync(predicate);
        }

        /// <summary>
        /// 直接删除（绕过软删除/审计拦截等），适用于清理数据或初始化脚本。
        /// 使用前请评估风险：该操作不可恢复，且可能绕过业务审计。
        /// </summary>
        /// <remarks>
        /// 风险与适用场景：
        /// - 风险：跳过软删标记与审计记录，物理删除后无法恢复；
        /// - 适用：测试数据清理、批量初始化/重置、已确认无需审计的场景。
        /// </remarks>
        public override async Task DeleteDirectAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken = default)
        {
            await DeleteAsync(predicate, true, cancellationToken);
        }

        public new async Task<ISugarQueryable<TEntity>> WithDetailsAsync()
        {
            if (EntityOptions.DefaultWithDetailsFunc == null)
            {
                throw new AbpInitializationException($"实体 {typeof(TEntity).Name} 未配置导航加载策略。");
            }

            var query = await GetSugarQueryableAsync();
            return EntityOptions.DefaultWithDetailsFunc(query);
        }
    }
}

public class SqlSugarRepository<TDbContext, TEntity, TKey> :
    SqlSugarRepository<TDbContext, TEntity>,
    ISqlSugarRepository<TEntity, TKey>
    where TDbContext : class, ISqlSugarDbContext
    where TEntity : class, IEntity<TKey>, new()
{
    public SqlSugarRepository(ISqlSugarDbContextProvider<TDbContext> provider)
        : base(provider)
    {
    }

    public virtual async Task<TEntity> GetAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var query = includeDetails ? await WithDetailsAsync() : await GetSugarQueryableAsync();
        return await query.InSingleAsync(id);
    }

    public virtual async Task<TEntity?> FindAsync(TKey id, bool includeDetails = true, CancellationToken cancellationToken = default)
    {
        var query = includeDetails ? await WithDetailsAsync() : await GetSugarQueryableAsync();
        return await query.InSingleAsync(id);
    }

    public virtual async Task DeleteAsync(TKey id, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        await db.Client.Deleteable<TEntity>().In(id).ExecuteCommandAsync(cancellationToken);
    }

    public virtual async Task DeleteManyAsync(IEnumerable<TKey> ids, bool autoSave = false, CancellationToken cancellationToken = default)
    {
        var db = await GetDbContextAsync();
        await db.Client.Deleteable<TEntity>().In(ids).ExecuteCommandAsync(cancellationToken);
    }
}
