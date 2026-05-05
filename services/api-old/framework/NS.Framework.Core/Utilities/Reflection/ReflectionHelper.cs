namespace NS.Framework.Core.Utilities.Reflection;

/// <summary>
/// 反射操作工具类
/// </summary>
public static class ReflectionHelper
{
    /// <summary>
    /// 获取对象指定属性的值
    /// </summary>
    /// <param name="fieldName">属性名称</param>
    /// <param name="obj">对象实例</param>
    /// <returns>属性值的字符串表示，如果属性不存在或对象为空则返回null</returns>
    public static string? GetModelValue(string fieldName, object? obj)
    {
        if (obj == null || string.IsNullOrWhiteSpace(fieldName))
            return null;

        var prop = obj.GetType().GetProperty(fieldName);
        if (prop == null)
            return null;

        var value = prop.GetValue(obj);
        return value?.ToString();
    }

    /// <summary>
    /// 设置对象指定属性的值
    /// </summary>
    /// <param name="fieldName">属性名称</param>
    /// <param name="value">要设置的值</param>
    /// <param name="obj">对象实例</param>
    /// <returns>设置是否成功</returns>
    public static bool SetModelValue(string fieldName, object value, object? obj)
    {
        if (obj == null || string.IsNullOrWhiteSpace(fieldName))
            return false;

        var prop = obj.GetType().GetProperty(fieldName);
        if (prop == null || !prop.CanWrite)
            return false;

        prop.SetValue(obj, value);
        return true;
    }
}

