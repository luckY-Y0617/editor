using System.Text.RegularExpressions;

namespace NS.Framework.Core.Utilities.Slug;

public static class SlugHelper
{
    public static string FromName(string name)
    {
        // 转小写、trim
        name = name.Trim().ToLowerInvariant();

        // 去掉特殊字符，只保留字母/数字/中文/空格/-
        name = Regex.Replace(name, @"[^a-z0-9\u4e00-\u9fa5\s-]", "");

        // 空格换成 -
        name = Regex.Replace(name, @"\s+", "-");

        return name;
    }

    public static string Normalize(string code)
    {
        code = code.Trim().ToLowerInvariant();

        // 只保留 slug 允许的字符
        return Regex.Replace(code, @"[^a-z0-9-]", "");
    }
}
