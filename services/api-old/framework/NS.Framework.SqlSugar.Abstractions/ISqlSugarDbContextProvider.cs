namespace NS.Framework.SqlSugar.Abstractions
{
    /// <summary>
    /// SqlSugar 上下文提供器接口（对标 EFCore 的 IDbContextProvider）。
    /// 负责根据当前工作单元（UnitOfWork）与租户上下文（Tenant）
    /// 创建或复用 SqlSugar 上下文（TDbContext）。
    /// </summary>

    public interface ISqlSugarDbContextProvider<TDbContext>
        where TDbContext : class, ISqlSugarDbContext
    {
        /// <summary>
        /// 异步获取 SqlSugar 上下文（DbContext 风格实例）。
        /// 若当前存在活跃的 UnitOfWork，则会优先复用当前事务下的上下文。
        /// </summary>
        /// <returns>与当前工作单元（UoW）关联的 <typeparamref name="TDbContext"/> 实例。</returns>
        Task<TDbContext> GetDbContextAsync();

        /// <summary>
        /// 同步获取 SqlSugar 上下文。
        /// 一般仅用于非异步上下文（如控制台工具、初始化逻辑）；
        /// 在 Web 应用中建议使用 <see cref="GetDbContextAsync"/>。
        /// </summary>
        TDbContext GetDbContext();
    }
}