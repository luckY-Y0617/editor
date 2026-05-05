
namespace NS.Module.TenantManagement.Application.Contracts.Cache;

[Serializable]
public class TenantConfigurationCacheItem
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string NormalizedName { get; set; } = string.Empty;


    /// <summary>
    /// 默认 DbType（字符串形式，方便在配置里存储）
    /// </summary>
    public string? DbType { get; set; }

    /// <summary>
    /// 连接字符串字典：Name → Value
    /// </summary>
    public Dictionary<string, string> ConnectionStrings { get; set; } = new();
}
