using System;
using SqlSugar;
using Volo.Abp.Domain.Entities;
using Check = Volo.Abp.Check;

namespace NS.Module.TenantManagement.Domain;

[SugarTable("tenant_connection_string")]
public class TenantConnectionString : Entity<Guid>
{

    [SugarColumn(IsNullable = false)]
    public Guid TenantId { get; protected set; }


    [SugarColumn(Length = 128, IsNullable = false)]
    public string Name { get; protected set; } = string.Empty;


    [SugarColumn(IsNullable = false, ColumnDataType = "longtext")]
    public string Value { get; protected set; } = string.Empty;


    public TenantConnectionString() { }

    internal TenantConnectionString(
        Guid tenantId,
        string name,
        string value)
    {
        TenantId = tenantId;
        Name = Check.NotNullOrWhiteSpace(name, nameof(name));
        Value = Check.NotNullOrWhiteSpace(value, nameof(value));
    }

    internal void SetValue(string value)
    {
        Value = Check.NotNullOrWhiteSpace(value, nameof(value));
    }
}