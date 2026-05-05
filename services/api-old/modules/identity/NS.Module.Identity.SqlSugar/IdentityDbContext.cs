using NS.Framework.SqlSugar;
using NS.Framework.SqlSugar.Abstractions;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Module.Identity.SqlSugar;

/// <summary>
/// Identity 模块的 DbContext
/// 继承自 SqlSugarDbContext，自动获得基础设施配置（软删除、多租户、实体映射等）
/// </summary>
/// <remarks>
/// 新架构使用说明：
/// 如需个性化配置，可重写 BuildOptions() 方法声明配置需求，Factory 会负责实际组装。
/// 常见配置场景：
/// 1. 自定义 ExternalServices（EntityService、EntityNameService、缓存服务等）
/// 2. 自定义 RuntimeConfigure（查询过滤器、AOP、数据过滤器等）
/// 3. 自定义 MoreSettings（连接池、缓存等高级设置）
/// </remarks>
public class IdentityDbContext : SqlSugarDbContext
{
    /// <summary>
    /// 新架构构造函数（推荐）
    /// 只注入服务提供者，配置通过 BuildOptions() 声明，组装由 Factory 负责
    /// </summary>
    public IdentityDbContext(IAbpLazyServiceProvider lazyServiceProvider)
        : base(lazyServiceProvider)
    {
    }

  
}
