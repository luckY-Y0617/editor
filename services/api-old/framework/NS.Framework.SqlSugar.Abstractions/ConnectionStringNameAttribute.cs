namespace NS.Framework.SqlSugar.Abstractions
{
    /// <summary>
    /// 指定当前 SqlSugar DbContext 对应的连接字符串名称。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ConnectionStringNameAttribute : Attribute
    {
        /// <summary>
        /// 配置文件中 ConnectionStrings 的 key。
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// 创建连接名特性。
        /// </summary>
        /// <param name="name">连接字符串名称，如 "Default"、"Audit"、"TenantDb"。</param>
        public ConnectionStringNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// 从类型获取连接字符串名（若无特性则返回 null）。
        /// </summary>
        public static string? GetConnStringName(Type dbContextType)
        {
            var attr = dbContextType.GetCustomAttributes(typeof(ConnectionStringNameAttribute), true);
            return attr.Length > 0
                ? ((ConnectionStringNameAttribute)attr[0]).Name
                : null;
        }
    }
}