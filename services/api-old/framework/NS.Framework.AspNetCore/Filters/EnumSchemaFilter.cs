using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace NS.Framework.AspNetCore.Filters;

/// <summary>
/// 枚举 Schema 过滤器：
/// 1. 在 Swagger 中将枚举渲染为 string
/// 2. 添加更清晰的 Markdown 表格说明（名称、值、描述）
/// 3. 自动读取 Description, Display, EnumMember
/// 4. 支持 Flags 枚举
/// </summary>
public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        var type = context.Type;

        // 非枚举则跳过
        if (!type.IsEnum)
            return;

        // 输出为字符串
        schema.Type = "string";
        schema.Format = null;
        schema.Enum.Clear();

        // 获取所有枚举字段
        var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static);

        // 添加到 swagger schema.Enum
        foreach (var f in fields)
        {
            schema.Enum.Add(new OpenApiString(f.Name));
        }

        // 生成 Markdown 表格
        schema.Description = BuildEnumMarkdownDescription(type, fields);
    }

    private static string BuildEnumMarkdownDescription(Type enumType, FieldInfo[] fields)
    {
        var sb = new StringBuilder();
        sb.AppendLine("枚举说明：");
        sb.AppendLine();
        sb.AppendLine("| 名称 | 数值 | 描述 |");
        sb.AppendLine("|------|------|------|");

        foreach (var f in fields)
        {
            var name = f.Name;
            var value = Convert.ToInt64(f.GetValue(null));
            var desc = GetDescription(f) ?? "";

            sb.AppendLine($"| {name} | {value} | {desc} |");
        }

        // 如果是 Flags 枚举，增加提示
        if (enumType.GetCustomAttribute<FlagsAttribute>() != null)
        {
            sb.AppendLine();
            sb.AppendLine("此枚举允许**位运算组合 (Flags)**。");
        }

        return sb.ToString();
    }

    /// <summary>
    /// 获取 Description / Display / EnumMember 中的描述
    /// </summary>
    private static string? GetDescription(FieldInfo field)
    {
        // DescriptionAttribute
        var description = field.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (!string.IsNullOrWhiteSpace(description))
            return description;

        // DisplayAttribute
        var display = field.GetCustomAttribute<DisplayAttribute>()?.Name;
        if (!string.IsNullOrWhiteSpace(display))
            return display;

        // EnumMemberAttribute
        var enumMember = field.GetCustomAttribute<EnumMemberAttribute>()?.Value;
        if (!string.IsNullOrWhiteSpace(enumMember))
            return enumMember;

        return null;
    }
}
