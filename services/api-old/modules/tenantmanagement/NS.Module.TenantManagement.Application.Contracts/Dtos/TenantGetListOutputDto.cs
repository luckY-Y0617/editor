using System;
using NS.Module.TenantManagement.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Application.Dtos;

namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

public class TenantGetListOutputDto : EntityDto<Guid>
{
    public string Name { get; set; } = string.Empty;

    public TenantProvisioningState ProvisioningState { get; set; }

    public Guid? EditionId { get; set; }

    public DbType DbType { get; set; }

    public DateTime CreationTime { get; set; }
}
