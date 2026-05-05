namespace NS.Framework.Core.Abstractions.Domain;

/// <summary>
/// 定义状态接口
/// </summary>
public interface IState
{
    /// <summary>
    /// 是否启用
    /// </summary>
    bool IsEnabled { get; }
}

