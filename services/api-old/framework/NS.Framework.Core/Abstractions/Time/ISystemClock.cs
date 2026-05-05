namespace NS.Framework.Core.Abstractions.Time;

public interface ISystemClock
{
    /// <summary>
    /// 获取当前本地时间
    /// </summary>
    DateTime Now { get; }
    
    /// <summary>
    /// 获取当前UTC时间
    /// </summary>
    DateTime UtcNow { get; }
}

public sealed class SystemClock : ISystemClock
{
    /// <inheritdoc />
    public DateTime Now => DateTime.Now;
    
    /// <inheritdoc />
    public DateTime UtcNow => DateTime.UtcNow;
}

