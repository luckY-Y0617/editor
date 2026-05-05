namespace NS.Framework.SqlSugar.Abstractions
{
    /// <summary>
    /// SqlSugar 上下文工厂接口。
    /// 
    /// 负责根据连接名称、连接字符串、数据库类型等信息，
    /// 创建并初始化 SqlSugar 上下文（TDbContext）。
    /// 
    /// 一般由 <see cref="ISqlSugarDbContextProvider{TDbContext}"/> 调用，
    /// 也可在特殊场景下（如初始化任务）手动使用。
    /// </summary>
    public interface ISqlSugarDbContextFactory
    {
        /// <summary>
        /// 异步创建指定类型的 SqlSugar 上下文。
        /// </summary>
        /// <typeparam name="TContext">上下文类型，如 SqlSugarClient 或自定义包装类。</typeparam>
        /// <param name="connectionName">连接名（如 "Default"）。</param>
        /// <returns>创建完成的 <typeparamref name="TContext"/> 实例。</returns>
        Task<TContext> CreateDbContextAsync<TContext>(string connectionName) where TContext : ISqlSugarDbContext;
    }
}