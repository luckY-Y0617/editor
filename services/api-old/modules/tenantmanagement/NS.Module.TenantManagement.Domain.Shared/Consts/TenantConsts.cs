namespace NS.Module.TenantManagement.Domain.Shared.Consts;

/// <summary>
/// 租户相关的基础常量。
/// </summary>
public static class TenantConsts
{
    /// <summary>
    /// 租户名称最大长度。
    /// </summary>
    public const int MaxNameLength = 64;

    /// <summary>
    /// 连接字符串名称最大长度。
    /// 例如："Default" / "Identity" / "Audit" 等。
    /// </summary>
    public const int MaxConnectionStringNameLength = 128;

    /// <summary>
    /// 模块统一命名前缀。
    /// </summary>
    public const string ModuleName = "ClayMo.TenantManagement";
}