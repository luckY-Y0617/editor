using System;

namespace NS.Module.TenantManagement.Application.Contracts.Dtos;

public sealed class TenantBootstrapStateDto
{
    public string? State { get; set; }
    public DateTime? ProvisionedAtUtc { get; set; }
    public string? LastError { get; set; }
}