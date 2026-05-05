using System;
using NS.Module.TenantManagement.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Domain.Entities;

namespace NS.Module.TenantManagement.Domain;

[SugarTable("tenant_provisioning_job")]
public  class TenantProvisioningJob:  Entity<Guid>, IHasConcurrencyStamp
{
    [SugarColumn(IsPrimaryKey = true)]
    public Guid TenantId { get; set; }

    public TenantProvisioningJobState State { get; set; } = TenantProvisioningJobState.Queued;

    [SugarColumn(IsNullable = true, Length = 64)]
    public string? LockOwner { get; set; }

    [SugarColumn(IsNullable = true)]
    public DateTime? LockUntilUtc { get; set; }

    public int AttemptCount { get; set; } = 0;

    [SugarColumn(IsNullable = true)]
    public DateTime? NextRunAtUtc { get; set; } = DateTime.UtcNow;

    [SugarColumn(IsNullable = true, Length = 2000)]
    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    public string ConcurrencyStamp { get; set; } =  Guid.NewGuid().ToString("N");

    public TenantProvisioningJob() { }
}