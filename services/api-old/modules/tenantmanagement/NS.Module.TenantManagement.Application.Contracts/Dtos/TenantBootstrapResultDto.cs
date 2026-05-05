using System;

namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

public sealed class TenantBootstrapResultDto
{
    public Guid TenantId { get; set; }
    public bool Succeeded { get; set; }
    public string? ErrorMessage { get; set; }

    public TenantBootstrapStateDto State { get; set; } = new();
    public TenantGetOutputDto? Tenant { get; set; }
}