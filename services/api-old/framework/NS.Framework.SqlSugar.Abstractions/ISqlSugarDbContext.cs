using SqlSugar;

namespace NS.Framework.SqlSugar.Abstractions;

/// <summary>
/// SqlSugar DbContext 抽象：
/// - 对外暴露已组装好的 ISqlSugarClient
/// - 由 Factory 负责创建与注入 Client
/// - DbContext 自身只声明配置（BuildOptions）
/// </summary>
public interface ISqlSugarDbContext
{
    /// <summary>
    /// 已组装完成的 SqlSugarClient（由 Factory 注入）
    /// </summary>
    ISqlSugarClient Client { get; set; }
    
    /// <summary>
    /// 声明式配置入口：DbContext（及其派生类）在此声明个性化配置需求，
    /// Factory 会结合 Contributors 的通用能力一起组装 SqlSugarClient。
    /// </summary>
    SqlSugarClientContext BuildOptions(string connectionString, DbType dbType);
}