using NS.Framework.SqlSugar;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.TenantManagement.Domain;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.MultiTenancy;

namespace NS.Module.TenantManagement.SqlSugar
{
    public class SqlSugarConnectionResolver : ISqlSugarConnectionResolver
    {
        private readonly ICurrentTenant _currentTenant;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<SqlSugarDbConnectionOptions> _options;
        private readonly ILogger<SqlSugarConnectionResolver>? _logger;

        public SqlSugarConnectionResolver(
            ICurrentTenant currentTenant,
            IServiceProvider serviceProvider,
            IOptions<SqlSugarDbConnectionOptions> options,
            ILogger<SqlSugarConnectionResolver>? logger = null)
        {
            _currentTenant = currentTenant ?? throw new ArgumentNullException(nameof(currentTenant));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _logger = logger;
        }

        [Obsolete("Use ResolveAsync method.")]
        public (string ConnectionString, DbType DbType) Resolve(string? connectionName = null)
        {
            return ResolveAsync(connectionName).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 异步解析连接字符串与 DbType（首选）。
        /// </summary>
        public async Task<(string ConnectionString, DbType DbType)> ResolveAsync(string? connectionName = null)
        {
            connectionName ??= _options.Value.DefaultConnectionName;

            // 1.多租户优先
            if (_currentTenant.Id.HasValue)
            {
                var tenantId = _currentTenant.Id.Value;

                var tenantConfig = await FindTenantConfigurationAsync(tenantId);

                if (tenantConfig is not TenantConfigurationWithDetails details)
                {
                    throw new UserFriendlyException(
                        $"未找到租户 {tenantId} 的配置信息（TenantConfigurationWithDetails）");
                }

                if (tenantConfig.ConnectionStrings is not IReadOnlyDictionary<string, string> dict)
                {
                    throw new UserFriendlyException(
                        $"租户 {tenantId} 的连接字符串配置无效");
                }

                // 1.1 必须明确配置该 connectionName，不再回退 Default
                if (!dict.TryGetValue(connectionName, out var connStr) ||
                    string.IsNullOrWhiteSpace(connStr))
                {
                    throw new UserFriendlyException(
                        $"租户 {tenantId} 缺少名为 '{connectionName}' 的连接字符串，请在租户后台补充配置");
                }

                // 1.2 DbType 必须由租户自己配置，不能用 Host 兜底
                if (string.IsNullOrWhiteSpace(details.DbType))
                {
                    throw new UserFriendlyException(
                        $"租户 {tenantId} 未配置 DbType，请在租户后台为该租户设置统一数据库类型");
                }

                if (!Enum.TryParse<DbType>(details.DbType, ignoreCase: true, out var dbType))
                {
                    throw new UserFriendlyException(
                        $"租户 {tenantId} 的 DbType 配置无效：{details.DbType}。请修改为合法值（例如：MySql、SqlServer、PostgreSQL 等）");
                }

                return (connStr, dbType);
            }

            // 2️.Host 层：只解析 Host 自己的连接串和 DbType
            return ResolveFromHostOptions(connectionName);
        }

        private (string ConnectionString, DbType DbType) ResolveFromHostOptions(string connectionName)
        {
            var opts = _options.Value;

            if (!opts.ConnectionStrings.TryGetValue(connectionName, out var connStr) ||
                string.IsNullOrWhiteSpace(connStr))
            {
                throw new InvalidOperationException(
                    $"Host 层未配置名为 '{connectionName}' 的连接字符串。connectionName={connectionName}");
            }

            if (!opts.DbTypes.TryGetValue(connectionName, out var dbType))
            {
                throw new InvalidOperationException(
                    $"Host 层连接 '{connectionName}' 缺少数据库类型 (DbType) 配置。connectionName={connectionName}");
            }

            return (connStr, dbType);
        }

        /// <summary>
        /// 获取指定连接名的从库配置列表（同步版本）。
        /// 注意：从库配置仅从 Host 层配置读取，多租户场景下暂不支持租户级别的从库配置。
        /// </summary>
        public List<SlaveConnectionConfig> GetSlaveConnections(string connectionName)
        {
            var slaveConfigs = _options.Value.GetSlaveConnections(connectionName);
            LogSlaveConnections(connectionName, slaveConfigs);
            return slaveConfigs;
        }

        /// <summary>
        /// 获取指定连接名的从库配置列表（异步版本，预留多租户扩展）。
        /// 当前实现与同步版本相同，未来可扩展支持租户级别的从库配置。
        /// </summary>
        public Task<List<SlaveConnectionConfig>> GetSlaveConnectionsAsync(string connectionName)
        {
            // 当前实现：仅从 Host 层读取
            // 未来扩展：可在此处添加租户级别的从库配置读取逻辑
            // 例如：
            // if (_currentTenant.Id.HasValue)
            // {
            //     var tenantConfig = await FindTenantConfigurationAsync(_currentTenant.Id.Value);
            //     if (tenantConfig?.SlaveConnections != null && tenantConfig.SlaveConnections.TryGetValue(connectionName, out var tenantSlaves))
            //     {
            //         return tenantSlaves;
            //     }
            // }
            
            var slaveConfigs = _options.Value.GetSlaveConnections(connectionName);
            LogSlaveConnections(connectionName, slaveConfigs);
            return Task.FromResult(slaveConfigs);
        }

        private void LogSlaveConnections(string connectionName, List<SlaveConnectionConfig>? slaveConfigs)
        {
            var tenantId = _currentTenant.Id?.ToString() ?? "Host";

            if (slaveConfigs is not { Count: > 0 })
            {
                _logger?.LogTrace(
                    "No slave connections configured. ConnectionName={ConnectionName}, Tenant={Tenant}",
                    connectionName,
                    tenantId
                );
                return;
            }

            var totalHitRate = slaveConfigs.Sum(s => s.HitRate);

            _logger?.LogDebug(
                "Resolved {Count} slave connections. ConnectionName={ConnectionName}, Tenant={Tenant}, TotalHitRate={TotalHitRate}",
                slaveConfigs.Count,
                connectionName,
                tenantId,
                totalHitRate
            );
        }
        
        private async Task<TenantConfiguration?> FindTenantConfigurationAsync(Guid tenantId)
        {
            using (var serviceScope = _serviceProvider.CreateScope())
            {
                var tenantStore = serviceScope
                    .ServiceProvider
                    .GetRequiredService<ITenantStore>();

                return await tenantStore.FindAsync(tenantId);
            }
        }

    }
}
