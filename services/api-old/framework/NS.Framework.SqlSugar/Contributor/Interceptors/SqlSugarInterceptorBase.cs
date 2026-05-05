using NS.Framework.SqlSugar.Abstractions;
using SqlSugar;

namespace NS.Framework.SqlSugar.Interceptors;

public abstract class SqlSugarInterceptorBase : ISqlSugarInterceptor
{
    public virtual int ExecutionOrder => 0;
    public virtual void OnLogExecuting(string sql, SugarParameter[] pars) {}
    public virtual void OnLogExecuted(string sql, SugarParameter[] pars) {}
    public virtual void DataExecuting(object value, DataFilterModel model) {}
    public virtual void DataExecuted(object value, DataAfterModel model) {}
}
