namespace NS.Framework.Core.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// 取日期部分（00:00:00）
    /// </summary>
    public static DateTime ToDate(this DateTime dateTime)
        => dateTime.Date;

    /// <summary>
    /// 获取所在周的开始日期
    /// </summary>
    public static DateTime GetWeekStart(
        this DateTime date,
        DayOfWeek firstDay = DayOfWeek.Monday)
    {
        var d = date.Date;
        var diff = (7 + (d.DayOfWeek - firstDay)) % 7;
        return d.AddDays(-diff);
    }
}
