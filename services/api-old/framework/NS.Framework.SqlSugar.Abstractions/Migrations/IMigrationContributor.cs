using SqlSugar;

namespace NS.Framework.SqlSugar.Abstractions.Migrations;

public interface IMigrationContributor
{
    /// <summary>
    /// 全局唯一 ID（建议：模块名/日期/动作），用于写入 schema history。
    /// 例：tenant-management/20260123_codefirst
    /// </summary>
    string Id { get; }

    /// <summary>
    /// 可读描述（日志用）
    /// </summary>
    string Description { get; }

    /// <summary>
    /// 是否为可重复（Repeatable）迁移：
    /// - false：只执行一次（history 存在即跳过）
    /// - true：checksum 变化才会重跑
    /// </summary>
    bool IsRepeatable { get; }


    /// <summary>
    /// 执行迁移（db已定位到目标库）
    /// </summary>
    Task ExecuteAsync(ISqlSugarClient db, CancellationToken cancellationToken = default);
}