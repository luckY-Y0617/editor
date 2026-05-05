using System;
using System.Text.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using JsonException = System.Text.Json.JsonException;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace NS.Framework.Core.Utilities.Json;

/// <summary>
/// JSON序列化和反序列化工具类
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// 解析JSON字符串为指定类型对象
    /// </summary>
    /// <typeparam name="T">目标类型</typeparam>
    /// <param name="json">JSON字符串</param>
    /// <returns>解析后的对象</returns>
    /// <exception cref="ArgumentException">当JSON字符串为空时抛出</exception>
    /// <exception cref="JsonException">当反序列化失败时抛出</exception>
    public static T ParseJson<T>(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("JSON string is null or empty.", nameof(json));

        var result = JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result == null)
            throw new JsonException($"Failed to deserialize JSON to type {typeof(T).FullName}");

        return result;
    }

    /// <summary>
    /// 序列化对象为JSON字符串，支持自定义日期时间格式
    /// </summary>
    /// <typeparam name="T">对象类型</typeparam>
    /// <param name="obj">要序列化的对象</param>
    /// <param name="dateTimeFormat">日期时间格式字符串</param>
    /// <returns>JSON字符串</returns>
    public static string SerializeWithDateFormat<T>(T obj, string dateTimeFormat)
    {
        IsoDateTimeConverter timeConverter = new IsoDateTimeConverter()
        {
            DateTimeFormat = dateTimeFormat
        };
        return JsonConvert.SerializeObject(obj, Formatting.Indented, timeConverter);
    }
}

