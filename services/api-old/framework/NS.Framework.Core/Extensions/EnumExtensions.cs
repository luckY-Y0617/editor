using System;
using System.Reflection;
using System.Runtime.Serialization;

namespace NS.Framework.Core.Extensions;

public static class EnumExtensions
{
    public static string ToContractString(this Enum value)
    {
        var type = value.GetType();
        var name = Enum.GetName(type, value);
        if (name == null) return value.ToString();

        var field = type.GetField(name);
        if (field == null) return name;

        var attr = field.GetCustomAttribute<EnumMemberAttribute>();
        return attr?.Value ?? name;
    }
}