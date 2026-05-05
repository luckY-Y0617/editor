using System;
using System.Collections.Generic;
using NS.Module.TenantManagement.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Application.Dtos;

namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

/// <summary>
/// 单个租户详情输出
/// </summary>
public class TenantGetOutputDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public DbType DbType { get; set; }

    public TenantProvisioningState State { get; set; }

    public Guid? EditionId { get; set; }

    public List<TenantConnectionStringDto> ConnectionStrings { get; set; } = new();

    public DateTime CreationTime { get; set; }
}