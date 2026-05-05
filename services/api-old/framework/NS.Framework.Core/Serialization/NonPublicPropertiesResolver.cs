using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NS.Framework.Core.Serialization;

/// <summary>
/// 支持私有属性的JSON序列化解析器
/// </summary>
public class NonPublicPropertiesResolver : DefaultContractResolver
{
    /// <inheritdoc />
    protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
    {
        var prop = base.CreateProperty(member, memberSerialization);
        if (member is System.Reflection.PropertyInfo pi)
        {
            prop.Readable = pi.GetMethod != null; // 有 Get 方法，表示可读
            prop.Writable = pi.SetMethod != null; // 有 Set 方法，表示可写
        }
        return prop;
    }
}

