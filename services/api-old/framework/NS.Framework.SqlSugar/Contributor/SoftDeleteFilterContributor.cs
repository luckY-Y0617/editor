using NS.Framework.SqlSugar.Abstractions;
using Volo.Abp;
using Volo.Abp.Data;

namespace NS.Framework.SqlSugar.Contributor;

/// <summary>
/// 通用软删除过滤器（遵循 ABP IDataFilter 开关）
/// </summary>
public sealed class SoftDeleteFilterContributor : ISqlSugarClientContributor
{
    public int ExecutionOrder => -500;

    public void Contribute(SqlSugarDbContextConfigurationContext context, SqlSugarClientContext options)
    {
        var dataFilter = context.ServiceProvider.GetService(typeof(IDataFilter)) as IDataFilter;
        if (dataFilter == null) return;

        options.AppendRuntime(client =>
        {
            if (dataFilter.IsEnabled<ISoftDelete>())
            {
                client.QueryFilter.AddTableFilter<ISoftDelete>(e => !e.IsDeleted);
            }
        });
    }
}