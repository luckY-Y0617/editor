using System;

namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

public class TenantGetListInputDto
{
    public string? Name { get; set; }

    public DateTime? StartTime { get; set; }

    public DateTime? EndTime { get; set; }
}