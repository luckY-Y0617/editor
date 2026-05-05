using System;
using NS.Framework.SqlSugar.Abstractions;
using Volo.Abp.Uow;

namespace NS.Framework.SqlSugar.Uow
{
    public class SqlSugarDatabaseApi : IDatabaseApi, IDisposable
    {
        public object DbContext { get; }

        /// <summary>
        /// 唯一连接标识，用于在UoW中区分不同连接。
        /// </summary>
        public string ConnectionKey { get; }

        public SqlSugarDatabaseApi(object dbContext)
        {
            DbContext = dbContext;

            if (dbContext is not ISqlSugarDbContext sugarCtx)
                throw new InvalidOperationException("DbContext must implement ISqlSugarDbContext.");

            var config = sugarCtx.Client.CurrentConnectionConfig
                         ?? throw new InvalidOperationException("SqlSugar CurrentConnectionConfig is null.");

            if (string.IsNullOrWhiteSpace(config.ConnectionString))
                throw new InvalidOperationException("SqlSugar ConnectionString is null or empty.");

            ConnectionKey = config.ConnectionString.Trim().ToLowerInvariant().Replace(" ", "");
        }

        public void Dispose()
        {
            try
            {
                if (DbContext is IDisposable d)
                {
                    d.Dispose();
                }
            }
            catch
            {
                // 忽略释放异常
            }
        }
    }
}