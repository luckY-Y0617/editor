using System.IO;
using System.Text;

namespace NS.Framework.Core.Utilities.Stream;

/// <summary>
/// 流操作工具类
/// </summary>
public static class StreamHelper
{
    /// <summary>
    /// 将字符串转换为内存流
    /// </summary>
    /// <param name="str">要转换的字符串</param>
    /// <returns>包含字符串内容的内存流</returns>
    public static System.IO.Stream StringToStream(string str)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(str);
        return new MemoryStream(bytes);
    }
}

