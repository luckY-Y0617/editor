namespace NS.Framework.SqlSugar.Abstractions;

/// <summary>
/// Options 贡献者：用于注入“通用能力”（横切能力），例如：默认实体映射、软删过滤器、慢 SQL、日志等。
/// 不负责创建 SqlSugarClient，不负责连接解析；只对 SqlSugarClientContext 做声明式追加。
/// </summary>
public interface ISqlSugarClientContributor
{
    /// <summary>
    /// 执行顺序：越小越先执行。建议通用基础能力使用较小值。
    /// </summary>
    int ExecutionOrder { get; }

    /// <summary>
    /// 对 Options 进行贡献（追加/声明）。应保持幂等：重复执行不会导致重复挂载。
    /// </summary>
    void Contribute(SqlSugarDbContextConfigurationContext context, SqlSugarClientContext options);
}