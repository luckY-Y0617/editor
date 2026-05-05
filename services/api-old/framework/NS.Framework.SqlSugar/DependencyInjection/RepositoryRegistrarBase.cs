using System;
using System.Collections.Generic;
using System.Linq;
using NS.Framework.SqlSugar.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Domain.Repositories;

namespace NS.Framework.SqlSugar.DependencyInjection;

/// <summary>
/// Repository 注册器基类
/// </summary>
public abstract class RepositoryRegistrarBase<TOptions>
    where TOptions : class
{
    protected TOptions Options { get; }

    protected RepositoryRegistrarBase(TOptions options)
    {
        Options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// 注册仓储
    /// </summary>
    public virtual void RegisterRepositories(IServiceCollection services, Type dbContextType)
    {
        var entityTypes = GetEntityTypes(dbContextType).ToList();

        foreach (var entityType in entityTypes)
        {
            // 注册 IRepository<TEntity>
            RegisterRepository(services, dbContextType, entityType);

            // 注册 IRepository<TEntity, TKey>
            var primaryKeyType = GetPrimaryKeyType(entityType);
            if (primaryKeyType != null)
            {
                RegisterRepository(services, dbContextType, entityType, primaryKeyType);
            }
        }
    }

    /// <summary>
    /// 获取实体类型列表
    /// </summary>
    protected abstract IEnumerable<Type> GetEntityTypes(Type dbContextType);

    /// <summary>
    /// 获取 Repository 类型（无主键）
    /// </summary>
    protected abstract Type GetRepositoryType(Type dbContextType, Type entityType);

    /// <summary>
    /// 获取 Repository 类型（有主键）
    /// </summary>
    protected abstract Type GetRepositoryType(Type dbContextType, Type entityType, Type primaryKeyType);

    /// <summary>
    /// 注册单个仓储（无主键）
    /// </summary>
    protected virtual void RegisterRepository(IServiceCollection services, Type dbContextType, Type entityType)
    {
        var repositoryType = GetRepositoryType(dbContextType, entityType);
        var repositoryInterface = typeof(IRepository<>).MakeGenericType(entityType);

        // 注册 IRepository<TEntity> -> Repository<TDbContext, TEntity>
        services.AddTransient(repositoryInterface, repositoryType);

        // 如果实现了 ISqlSugarRepository<TEntity>，也注册它
        var sqlSugarRepositoryInterface = typeof(ISqlSugarRepository<>).MakeGenericType(entityType);
        if (sqlSugarRepositoryInterface.IsAssignableFrom(repositoryType))
        {
            services.AddTransient(sqlSugarRepositoryInterface, repositoryType);
        }
    }

    /// <summary>
    /// 注册单个仓储（有主键）
    /// </summary>
    protected virtual void RegisterRepository(IServiceCollection services, Type dbContextType, Type entityType, Type primaryKeyType)
    {
        var repositoryType = GetRepositoryType(dbContextType, entityType, primaryKeyType);
        var repositoryInterface = typeof(IRepository<,>).MakeGenericType(entityType, primaryKeyType);

        // 注册IRepository<TEntity, TKey>->Repository<TDbContext, TEntity, TKey>
        services.AddTransient(repositoryInterface, repositoryType);

        // 如果实现了 ISqlSugarRepository<TEntity, TKey>，也注册它
        var sqlSugarRepositoryInterface = typeof(ISqlSugarRepository<,>).MakeGenericType(entityType, primaryKeyType);
        if (sqlSugarRepositoryInterface.IsAssignableFrom(repositoryType))
        {
            services.AddTransient(sqlSugarRepositoryInterface, repositoryType);
        }
    }

    /// <summary>
    /// 获取实体的主键类型
    /// </summary>
    protected virtual Type? GetPrimaryKeyType(Type entityType)
    {
        // 检查是否实现了 IEntity<TKey>
        var entityInterface = entityType
            .GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEntity<>));

        if (entityInterface != null)
        {
            return entityInterface.GetGenericArguments()[0];
        }

        return null;
    }
}

