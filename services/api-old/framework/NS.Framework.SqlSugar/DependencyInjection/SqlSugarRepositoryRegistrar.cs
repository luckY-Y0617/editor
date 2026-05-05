using System;
using System.Collections.Generic;
using Volo.Abp.Domain.Repositories;

namespace NS.Framework.SqlSugar.DependencyInjection;

/// <summary>
/// SqlSugar Repository 注册器
/// 参考 ABP 的 EfCoreRepositoryRegistrar 实现
/// </summary>
public class SqlSugarRepositoryRegistrar : RepositoryRegistrarBase<SqlSugarDbContextRegistrationOptions>
{
    public SqlSugarRepositoryRegistrar(SqlSugarDbContextRegistrationOptions options)
        : base(options)
    {
    }

    protected override IEnumerable<Type> GetEntityTypes(Type dbContextType)
    {
        return SqlSugarDbContextHelper.GetEntityTypes(dbContextType);
    }

    protected override Type GetRepositoryType(Type dbContextType, Type entityType)
    {
        return typeof(SqlSugarRepository<,>).MakeGenericType(dbContextType, entityType);
    }

    protected override Type GetRepositoryType(Type dbContextType, Type entityType, Type primaryKeyType)
    {
        return typeof(SqlSugarRepository<,,>).MakeGenericType(dbContextType, entityType, primaryKeyType);
    }
}

