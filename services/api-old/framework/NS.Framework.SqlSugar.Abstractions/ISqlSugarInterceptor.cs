using SqlSugar;

namespace NS.Framework.SqlSugar.Abstractions
{
    public interface ISqlSugarInterceptor
    {
        int ExecutionOrder { get; }
        /// <summary>
        /// sql执行前
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pars"></param>
        void OnLogExecuting(string sql, SugarParameter[] pars);
        
        /// <summary>
        /// sql执行后
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="pars"></param>
        void OnLogExecuted(string sql, SugarParameter[] pars);
        
        /// <summary>
        /// db数据读取前
        /// </summary>
        /// <param name="value"></param>
        /// <param name="model"></param>
        void DataExecuting(object value, DataFilterModel model);
        
        /// <summary>
        /// db数据读取后
        /// </summary>
        /// <param name="value"></param>
        /// <param name="model"></param>
        void DataExecuted(object value, DataAfterModel model);
    }
}
