using NS.Framework.SqlSugar.Abstractions;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.SqlSugar;

public class SqlSugarDbContext : ISqlSugarDbContext
{
    public ISqlSugarClient Client { get; set; } = default!;

    protected IAbpLazyServiceProvider LazyServiceProvider { get; }

    public SqlSugarDbContext(IAbpLazyServiceProvider lazyServiceProvider)
    {
        LazyServiceProvider = lazyServiceProvider;
    }

    /// <summary>
    /// 声明式配置（核心入口）
    /// - 基类只提供“扩展点 + 默认空实现”
    /// - 通用能力（默认 EntityService、软删/多租户过滤器、通用拦截器等）全部交给 Contributor
    /// - 子类只在这里声明“个性化配置”
    ///
    /// 注意：这里不要直接写通用逻辑，也不要在这里解析 DI 去挂拦截器（那是 Factory/Contributor 的责任）
    /// </summary>
    public virtual SqlSugarClientContext BuildOptions(string connectionString, DbType dbType)
    {
        // 默认只给一个安全默认；通用项由 contributor 注入
        var options = SqlSugarClientContext.Create();

        // 子类声明个性化 ExternalServices（如表名策略、字段映射、缓存等）
        ConfigureExternalServices(options, connectionString, dbType);

        // 子类声明个性化 Runtime（如数据权限过滤器、模块 AOP 等）
        ConfigureRuntime(options, connectionString, dbType);

        // 子类声明“仅此 DbContext 需要的拦截器”（类型声明即可）
        ConfigureInterceptors(options, connectionString, dbType);

        return options;
    }

    /// <summary>
    /// 个性化外部服务：子类通过 options.AppendExternalServices(...) 追加配置
    /// </summary>
    protected virtual void ConfigureExternalServices(SqlSugarClientContext options, string connectionString, DbType dbType)
    {
        // 默认不做任何事（通用外部服务由 contributor 提供）
        // 子类示例：
        // options.AppendExternalServices(es => es.EntityNameService = (t, e) => { ... });
    }

    /// <summary>
    /// 个性化运行时配置：子类通过 options.AppendRuntime(...) 追加配置
    /// </summary>
    protected virtual void ConfigureRuntime(SqlSugarClientContext options, string connectionString, DbType dbType)
    {
        // 默认不做任何事（通用过滤器/通用 AOP 由 contributor 提供）
        // 子类示例：
        // options.AppendRuntime(client => { client.QueryFilter.AddTableFilter<...>(...); });
    }

    /// <summary>
    /// 个性化拦截器声明：子类通过 options.AddInterceptor(...) 声明需要挂载的拦截器类型
    /// </summary>
    protected virtual void ConfigureInterceptors(SqlSugarClientContext options, string connectionString, DbType dbType)
    {
        // 默认不做任何事（通用拦截器由 contributor 提供）
        // 子类示例：
        // options.AddInterceptor<MyModuleAuditInterceptor>();
    }
}
