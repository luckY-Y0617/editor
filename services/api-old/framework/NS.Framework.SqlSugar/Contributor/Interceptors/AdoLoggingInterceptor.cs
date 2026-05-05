using System.Linq;
using Microsoft.Extensions.Logging;
using SqlSugar;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.SqlSugar.Interceptors;

public class AdoLoggingInterceptor: SqlSugarInterceptorBase, ITransientDependency
{
    private readonly ILogger _logger;

    public AdoLoggingInterceptor(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger("SqlSugar.Ado");
    }

    public override int ExecutionOrder => 0;

    public override void OnLogExecuting(string sql, SugarParameter[] pars)
    {
        _logger.LogDebug("Executing SQL: {Sql}\nParams: {Params}",
            sql,
            string.Join(", ", pars.Select(p => $"{p.ParameterName}={p.Value}")));
    }
}