using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.SqlSugar.Abstractions
{
    /// <summary>
    /// 提供给特定 "DbContext 风格" SqlSugar 上下文的配置上下文。
    /// 包含 ServiceProvider、连接字符串、配置名、逻辑数据源键（DataSourceKey）、
    /// 多租户标识（TenantId）、数据库类型（DbType）以及待配置的 SqlSugarClient 实例。
    ///
    /// 设计简化说明：
    /// - ConfigureExternalServices 配置已移至 ISqlSugarDbContext.ConfigureExternalServices() 静态方法
    /// - 避免了不必要的 API 复杂性，职责更加清晰
    /// </summary>
    public class SqlSugarDbContextConfigurationContext : IServiceProviderAccessor
    {
        public IServiceProvider ServiceProvider { get; }

        /// <summary>实际的数据库连接字符串（解析后的）</summary>
        public string ConnectionString { get; }

        /// <summary>配置文件中 ConnectionStrings 的 key</summary>
        public string ConnectionStringName { get; }

        /// <summary>
        /// 逻辑运行时数据源标识, 默认为 ConnectionStringName
        /// </summary>
        public string DataSourceKey { get; }

        /// <summary>数据库类型（SqlSugar.DbType）</summary>
        public DbType DbType { get; }

        /// <summary>可由Configure动作设置的客户端实例</summary>
        public SqlSugarClient? Client { get; set; }

        /// <summary>
        /// 从库连接配置列表。用于读写分离场景。
        /// 如果未配置，则为空列表。
        /// </summary>
        public List<SlaveConnectionConfig>? SlaveConnectionConfigs { get; set; }

        public SqlSugarDbContextConfigurationContext(
            string connectionString,
            IServiceProvider serviceProvider,
            string connectionStringName,
            DbType dbType,
            string? dataSourceKey = null,
            List<SlaveConnectionConfig>? slaveConnectionConfigs = null)
        {
            ConnectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            ConnectionStringName = connectionStringName ?? throw new ArgumentNullException(nameof(connectionStringName));
            DataSourceKey = dataSourceKey ?? connectionStringName;
            SlaveConnectionConfigs = slaveConnectionConfigs ?? [];
            DbType = dbType;
        }
    }

    public class SqlSugarDbContextConfigurationContext<TContext> : SqlSugarDbContextConfigurationContext
    {
        public new SqlSugarClient? Client
        {
            get => base.Client;
            set => base.Client = value;
        }

        public SqlSugarDbContextConfigurationContext(
            string connectionString,
            IServiceProvider serviceProvider,
            string connectionStringName,
            DbType dbType,
            Guid? tenantId = null,
            string? dataSourceKey = null,
            List<SlaveConnectionConfig>? slaveConnectionConfigs = null)
            : base(connectionString, serviceProvider, connectionStringName, dbType, dataSourceKey, slaveConnectionConfigs)
        {
        }
    }
}
