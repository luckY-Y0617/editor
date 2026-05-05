using SqlSugar;

namespace NS.Framework.SqlSugar.Abstractions;

/// <summary>
/// SqlSugar 上下文配置选项（声明式配置）
///
/// 设计目标：
/// 2) 高扩展：DbContext 或模块可以通过显式 API 叠加运行时配置与外部服务配置
/// 3) 行为可预期：所有叠加顺序都由调用顺序决定，不引入隐式 Merge 规则
/// </summary>
public sealed class SqlSugarClientContext
{
    /// <summary>
    /// 连接级附加设置（缓存、性能优化等）
    /// 注意：该对象是可变引用类型；建议由调用方一次性设置或自己管理覆盖策略。
    /// </summary>
    public ConnMoreSettings? MoreSettings { get; private set; }

    /// <summary>
    /// 外部服务配置（实体映射、命名策略、缓存、SQL函数等）
    /// 注意：不做通用“深合并”，只允许显式追加或整体替换，避免隐蔽覆盖。
    /// </summary>
    public ConfigureExternalServices? ExternalServices { get; private set; }

    /// <summary>
    /// SqlSugarClient 创建后执行的运行时配置（QueryFilter、AOP 等）
    /// 多次追加时按追加顺序执行。
    /// </summary>
    public Action<ISqlSugarClient>? RuntimeConfigure { get; private set; }

    /// <summary>
    /// 声明要挂载的拦截器类型（从 DI 解析）
    /// 去重规则：按 Type 去重。
    /// 执行顺序：由拦截器自身 ExecutionOrder 决定（Factory attach 时排序）。
    /// </summary>
    public ISet<Type> InterceptorTypes { get; } = new HashSet<Type>();

    /// <summary>
    /// 自定义拦截器挂载策略（可选）。
    /// 如果设置，Factory 应优先使用该策略（完全接管挂载逻辑）。
    /// </summary>
    public Action<IServiceProvider, ISqlSugarClient>? AttachInterceptors { get; private set; }

    private SqlSugarClientContext() { }

    /// <summary>
    /// 创建一个“安全默认”的 Options（只放不易产生歧义的默认值）
    /// </summary>
    public static SqlSugarClientContext Create()
        => new SqlSugarClientContext()
            .WithMoreSettings(new ConnMoreSettings
            {
                IsAutoRemoveDataCache = true
            });

    public SqlSugarClientContext WithMoreSettings(ConnMoreSettings settings)
    {
        MoreSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        return this;
    }

    public SqlSugarClientContext WithExternalServices(ConfigureExternalServices services)
    {
        ExternalServices = services ?? throw new ArgumentNullException(nameof(services));
        return this;
    }

    public SqlSugarClientContext WithAttachInterceptors(Action<IServiceProvider, ISqlSugarClient> attach)
    {
        AttachInterceptors = attach ?? throw new ArgumentNullException(nameof(attach));
        return this;
    }

    public SqlSugarClientContext AppendExternalServices(Action<ConfigureExternalServices> configure)
    {
        ExternalServices ??= new ConfigureExternalServices();
        configure(ExternalServices);
        return this;
    }

    /// <summary>
    /// 显式追加运行时配置：按追加顺序执行。
    /// </summary>
    public SqlSugarClientContext AppendRuntime(Action<ISqlSugarClient> configure)
    {
        if (configure == null) throw new ArgumentNullException(nameof(configure));

        var previous = RuntimeConfigure;
        RuntimeConfigure = previous == null
            ? configure
            : (client) =>
            {
                previous(client);
                configure(client);
            };

        return this;
    }

    /// <summary>
    /// 声明挂载一个拦截器类型（从 DI 解析，按 Type 去重）。
    /// </summary>
    public SqlSugarClientContext AddInterceptor<TInterceptor>() => AddInterceptor(typeof(TInterceptor));

    public SqlSugarClientContext AddInterceptor(Type interceptorType)
    {
        if (interceptorType == null) throw new ArgumentNullException(nameof(interceptorType));
        InterceptorTypes.Add(interceptorType);
        return this;
    }

    public SqlSugarClientContext AddInterceptors(params Type[] interceptorTypes)
    {
        if (interceptorTypes == null) throw new ArgumentNullException(nameof(interceptorTypes));
        foreach (var t in interceptorTypes)
        {
            InterceptorTypes.Add(t);
        }
        return this;
    }


    public void ApplyRuntime(ISqlSugarClient client)
    {
        RuntimeConfigure?.Invoke(client);
    }

    /// <summary>
    /// Factory 可用于判断是否启用自定义挂载策略。
    /// </summary>
    public bool HasCustomInterceptorAttacher => AttachInterceptors != null;
}
