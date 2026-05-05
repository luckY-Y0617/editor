using System;
using System.Reflection;
using NS.Framework.SqlSugar.Abstractions;
using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;

namespace NS.Framework.SqlSugar.Contributor;

public sealed class DefaultEntityServiceContributor : ISqlSugarClientContributor
{
    public int ExecutionOrder => -1000;

    public void Contribute(SqlSugarDbContextConfigurationContext context, SqlSugarClientContext options)
    {
        // 只追加，不覆盖：如果 DbContext/其它 contributor 已经设置了 EntityService，就不抢
        options.AppendExternalServices(es =>
        {
            if (es.EntityService != null) return;
            es.EntityService = DefaultEntityService;
        });
    }

    private static void DefaultEntityService(PropertyInfo propertyInfo, EntityColumnInfo entityColumnInfo)
    {
        // Nullable<T> 可空列
        var underlyingType = Nullable.GetUnderlyingType(propertyInfo.PropertyType);
        if (underlyingType != null)
        {
            entityColumnInfo.IsNullable = true;
        }

        // 并发戳启用版本验证
        if (propertyInfo.Name == nameof(IHasConcurrencyStamp.ConcurrencyStamp))
        {
            entityColumnInfo.IsEnableUpdateVersionValidation = true;
        }

        // ExtraPropertyDictionary 忽略映射
        if (propertyInfo.PropertyType == typeof(ExtraPropertyDictionary))
        {
            entityColumnInfo.IsIgnore = true;
        }

        // Id 字段标记主键（兼容常见实体基类）
        if (propertyInfo.Name == "Id")
        {
            entityColumnInfo.IsPrimarykey = true;
        }
    }
}