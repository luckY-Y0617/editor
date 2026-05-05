using System.Collections.Generic;
using SqlSugar;

namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

public class TenantCreateDto
{
    public string Name { get; set; } = string.Empty;

    public DbType DbType { get; set; } = DbType.MySql;

    public string DefaultConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// 附加连接串列表（可包含或不包含 Default）
    /// </summary>
    public List<TenantConnectionStringDto>? ConnectionStrings { get; set; } = null!;
}