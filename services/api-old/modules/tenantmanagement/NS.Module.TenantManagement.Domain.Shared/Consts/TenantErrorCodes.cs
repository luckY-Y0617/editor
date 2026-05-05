namespace NS.Module.TenantManagement.Domain.Shared.Consts;

/// <summary>
/// 租户模块的业务错误码。
/// </summary>
public static class TenantErrorCodes
{
    private const string Prefix = TenantConsts.ModuleName + ":";

    /// <summary>
    /// 租户已存在（用于名称重复）。
    /// </summary>
    public const string TenantAlreadyExists = Prefix + "TenantAlreadyExists";

    /// <summary>
    /// 并发冲突（EntityVersion 不一致）。
    /// </summary>
    public const string TenantConcurrencyError = Prefix + "TenantConcurrencyError";

    /// <summary>
    /// 需要默认连接字符串（创建租户时未提供）。
    /// </summary>
    public const string DefaultConnectionStringRequired = Prefix + "DefaultConnectionStringRequired";

    /// <summary>
    /// 指定名称的连接字符串不存在。
    /// </summary>
    public const string ConnectionStringNotFound = Prefix + "ConnectionStringNotFound";
    
    public const string TenantNotFound = Prefix + "TenantNotFound";
    public const string TenantNotActive = Prefix + "TenantNotActive";
    
    public const string TenantNameRequired = Prefix + "TenantNameRequired";
}