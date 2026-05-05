using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;
using NS.Framework.SqlSugar.Abstractions;
using NS.Framework.SqlSugar.Uow;

namespace NS.Framework.SqlSugar
{
    public class UnitOfWorkDbContextProvider<TDbContext> :
        ISqlSugarDbContextProvider<TDbContext>, ITransientDependency
        where TDbContext : class, ISqlSugarDbContext
    {
        protected readonly IUnitOfWorkManager UnitOfWorkManager;
        protected readonly ICurrentTenant CurrentTenant;
        protected readonly ISqlSugarDbContextFactory DbContextFactory;
        protected readonly ILogger<UnitOfWorkDbContextProvider<TDbContext>> Logger;

        public UnitOfWorkDbContextProvider(
            IUnitOfWorkManager unitOfWorkManager,
            ICurrentTenant currentTenant,
            ISqlSugarDbContextFactory dbContextFactory,
            ILogger<UnitOfWorkDbContextProvider<TDbContext>> logger)
        {
            UnitOfWorkManager = unitOfWorkManager;
            CurrentTenant = currentTenant;
            DbContextFactory = dbContextFactory;
            Logger = logger;
        }

        public virtual async Task<TDbContext> GetDbContextAsync()
        {
            var unitOfWork = UnitOfWorkManager.Current
                ?? throw new AbpException("SqlSugarContext只能在工作单元内创建");

            var targetType = typeof(TDbContext);
            var connectionName = ConnectionStringNameAttribute.GetConnStringName(targetType) ?? "Default";
            var dbContextKey = $"{targetType.FullName}_{connectionName}_{CurrentTenant.Id?.ToString() ?? "host"}";

            // 尝试从当前 UoW 获取已存在的 DatabaseApi
            var databaseApi = unitOfWork.FindDatabaseApi(dbContextKey);

            if (databaseApi == null)
            {
                // 通过 Factory 创建上下文
                var context = await DbContextFactory.CreateDbContextAsync<TDbContext>(connectionName);

                var dbApi = new SqlSugarDatabaseApi(context);
                unitOfWork.AddDatabaseApi(dbContextKey, dbApi);
                databaseApi = dbApi;

                // 若工作单元启用事务，则自动开启
                if (unitOfWork.Options.IsTransactional)
                {
                    BeginTransaction(context, dbApi, connectionName);
                }
            }

            return (TDbContext)((SqlSugarDatabaseApi)databaseApi).DbContext;
        }

        [Obsolete("Use GetDbContextAsync() instead.")]
        public virtual TDbContext GetDbContext()
        {
            return GetDbContextAsync().GetAwaiter().GetResult();
        }

        protected virtual void BeginTransaction(TDbContext context, SqlSugarDatabaseApi databaseApi, string connectionName)
        {
            var uow = UnitOfWorkManager.Current!;
            var transactionApiKey = $"SqlSugar_{databaseApi.ConnectionKey}";

            if (uow.FindTransactionApi(transactionApiKey) is SqlSugarTransactionApi)
                return; 

            try
            {
                if (context is ISqlSugarDbContext sugarCtx)
                {
                    var client = sugarCtx.Client;

                    client.Ado.BeginTran();

                    var tranApi = new SqlSugarTransactionApi(
                        client,
                        Logger,
                        uow.Id,
                        transactionApiKey,
                        databaseApi.ConnectionKey,
                        connectionName,
                        CurrentTenant.Id?.ToString());
                    uow.AddTransactionApi(transactionApiKey, tranApi);

                    Logger.LogInformation(
                        "【SqlSugar事务开始】UowId = {uowId},DbContext={ContextType}, 租户={TenantId}, 连接名={ConnectionName}, 连接Key={ConnectionKey}",
                        uow.Id,
                        typeof(TDbContext).Name,
                        CurrentTenant.Id?.ToString() ?? "Host",
                        connectionName,
                        databaseApi.ConnectionKey
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning(
                    ex,
                    "【SqlSugar事务开始失败】DbContext={Context} | 租户={TenantId} | 连接名={ConnectionName} | ConnectionKey={ConnectionKey}",
                    typeof(TDbContext).Name,
                    (CurrentTenant.Id?.ToString() ?? "Host"),
                    connectionName,
                    databaseApi.ConnectionKey
                );

            }
        }
    }
}
