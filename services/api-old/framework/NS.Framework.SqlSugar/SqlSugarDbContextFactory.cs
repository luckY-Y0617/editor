using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NS.Framework.SqlSugar.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.SqlSugar;

public sealed class SqlSugarDbContextFactory : ISqlSugarDbContextFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ISqlSugarConnectionResolver _connectionResolver;
    private readonly IEnumerable<ISqlSugarClientContributor> _contributors;
    private readonly ILogger<SqlSugarDbContextFactory> _logger;

    public SqlSugarDbContextFactory(
        IServiceProvider serviceProvider,
        ISqlSugarConnectionResolver connectionResolver,
        IEnumerable<ISqlSugarClientContributor> contributors,
        ILogger<SqlSugarDbContextFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _connectionResolver = connectionResolver;
        _contributors = contributors;
        _logger = logger;
    }

    public async Task<TContext> CreateDbContextAsync<TContext>(string connectionName)
        where TContext : ISqlSugarDbContext
    {
        var (connectionString, dbType) = await _connectionResolver.ResolveAsync(connectionName);

        try
        {
            var lazyServiceProvider = _serviceProvider.GetRequiredService<IAbpLazyServiceProvider>();
            var dbContext = ActivatorUtilities.CreateInstance<TContext>(_serviceProvider, lazyServiceProvider);

            var options = dbContext.BuildOptions(connectionString, dbType);

            var cfgCtx = new SqlSugarDbContextConfigurationContext(
                connectionString: connectionString,
                serviceProvider: _serviceProvider,
                connectionStringName: connectionName,
                dbType: dbType,
                dataSourceKey: connectionName,
                slaveConnectionConfigs: null);

            // 注册公共能力
            foreach (var contributor in _contributors.OrderBy(x => x.ExecutionOrder))
            {
                contributor.Contribute(cfgCtx, options);
            }

            // 
            var config = new ConnectionConfig
            {
                ConnectionString = connectionString,
                DbType = dbType,
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute,
                ConfigureExternalServices = options.ExternalServices,
                MoreSettings = options.MoreSettings,
                SlaveConnectionConfigs = cfgCtx.SlaveConnectionConfigs
            };

            var client = new SqlSugarClient(config);
            cfgCtx.Client = client; 
            
            options.ApplyRuntime(client);

            dbContext.Client = client;

            return dbContext;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create SqlSugar DbContext {ContextType} with connection {ConnectionName}",
                typeof(TContext).Name, connectionName);
            throw;
        }
    }
}
