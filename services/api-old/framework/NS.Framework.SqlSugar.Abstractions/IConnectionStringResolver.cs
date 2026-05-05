using SqlSugar;

namespace NS.Framework.SqlSugar.Abstractions;

/// <summary>
/// SqlSugar 运行时连接解析器。
/// 负责根据连接名与租户上下文解析出最终的 (ConnectionString, DbType)。
/// 相当于 ABP 的 IConnectionStringResolver 的增强版。
/// </summary>
public interface ISqlSugarConnectionResolver
{
    [Obsolete("Use ResolveAsync method.")]
    (string ConnectionString, DbType DbType) Resolve(string? connectionName = null);

    Task<(string ConnectionString, DbType DbType)> ResolveAsync(string? connectionName = null);

    /// <summary>
    /// 获取指定连接名的从库配置列表（同步版本）。
    /// 注意：当前实现仅从 Host 层配置读取，多租户场景下暂不支持租户级别的从库配置。
    /// </summary>
    /// <param name="connectionName">连接名</param>
    /// <returns>从库配置列表，如果未配置则返回空列表</returns>
    List<SlaveConnectionConfig> GetSlaveConnections(string connectionName);

    /// <summary>
    /// 获取指定连接名的从库配置列表（异步版本，预留多租户扩展）。
    /// 当前实现与同步版本相同，未来可扩展支持租户级别的从库配置。
    /// </summary>
    /// <param name="connectionName">连接名</param>
    /// <returns>从库配置列表，如果未配置则返回空列表</returns>
    Task<List<SlaveConnectionConfig>> GetSlaveConnectionsAsync(string connectionName);
}