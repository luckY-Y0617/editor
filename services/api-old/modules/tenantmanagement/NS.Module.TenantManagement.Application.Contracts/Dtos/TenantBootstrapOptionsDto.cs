namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

public sealed class TenantBootstrapOptionsDto
{
    /// <summary>
    /// 可选：重新初始化时传入新的默认连接字符串（Name = "Default"）
    /// </summary>
    public string? DefaultConnectionString { get; set; }
}