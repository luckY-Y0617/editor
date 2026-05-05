using System;
using System.Collections.Generic;
using System.Linq;
using NS.Module.TenantManagement.Domain.Shared;
using NS.Module.TenantManagement.Domain.Shared.Enums;
using SqlSugar;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities.Auditing;
using Check = Volo.Abp.Check;

namespace NS.Module.TenantManagement.Domain;

[SugarTable("tenant")]
[SugarIndex("NormalizedName", nameof(NormalizedName), OrderByType.Asc, isUnique: true)]
public class TenantAggregateRoot : FullAuditedAggregateRoot<Guid>
{

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    /// <summary>
    /// 租户级默认 DbType
    /// </summary>
    public DbType DbType { get; private set; } = DbType.MySql;

    public TenantProvisioningState ProvisioningState { get; private set; } = TenantProvisioningState.NotReady;

    public DateTime? ProvisionedAtUtc { get; private set; }

    [SugarColumn(ColumnName = "LastProvisioningError", Length = 2000, IsNullable = true)]
    public string? LastProvisioningError { get; private set; }


    [SugarColumn(IsIgnore = true)]
    public override ExtraPropertyDictionary ExtraProperties
    {
        get => base.ExtraProperties;
        protected set => base.ExtraProperties = value;
    }
    
    [Navigate(NavigateType.OneToMany, nameof(TenantConnectionString.TenantId))]
    public List<TenantConnectionString>? ConnectionStrings { get; set; }


    #region 构造函数

    public TenantAggregateRoot() { }

    internal TenantAggregateRoot(
        string name,
        DbType dbType)
    {
        Name = name;
        NormalizedName = NormalizeName(name);
        DbType = dbType;
    }

    #endregion

    #region 内部状态变更（仅领域服务调用）
    
    public void MarkProvisioning()
    {
        ProvisioningState = TenantProvisioningState.Provisioning;
        LastProvisioningError = null;
        ProvisionedAtUtc = null;
    }

    public void MarkReady(DateTime utcNow)
    {
        ProvisioningState = TenantProvisioningState.Ready;
        ProvisionedAtUtc = utcNow;
        LastProvisioningError = null;
    }

    public void MarkFailed(string error, DateTime utcNow)
    {
        ProvisioningState = TenantProvisioningState.Failed;
        ProvisionedAtUtc = null;
        // 这里可按需截断，避免过长写库失败
        LastProvisioningError = error.Length > 2000 ? error[..2000] : error;
    }

    public void SetName(string name)
    {
        Name = name;
        NormalizedName = NormalizeName(name);
    }

    public void SetDbType(DbType dbType)
    {
        DbType = dbType;
    }

    #endregion

    #region 连接串集合操作（不做业务策略，只维护集合）

    internal TenantConnectionString? FindConnectionString(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        return ConnectionStrings?.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public void AddOrUpdateConnectionString(
        string name,
        string value)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));
        Check.NotNullOrWhiteSpace(value, nameof(value));

        ConnectionStrings ??= [];

        var existing = ConnectionStrings.FirstOrDefault(
            x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            existing.SetValue(value);
            return;
        }

        ConnectionStrings.Add(new TenantConnectionString(Id, name, value));
    }


    internal void RemoveConnectionString(string name)
    {
        if (ConnectionStrings is null)
        {
            return;
        }


        var cs = ConnectionStrings.FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        if (cs != null)
        {
            ConnectionStrings?.Remove(cs);
        }
    }

    internal void ClearConnectionStrings()
    {
        if (ConnectionStrings is null)
        {
            return;
        }
        
        ConnectionStrings.Clear();
    }

    #endregion


    public static string NormalizeName(string name)
    {
        var s = Check.NotNullOrWhiteSpace(name, nameof(name));
        return s.Trim().ToUpperInvariant();
    }

}
