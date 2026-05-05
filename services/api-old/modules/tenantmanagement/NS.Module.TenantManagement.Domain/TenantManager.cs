using System;
using System.Collections.Generic;
using SqlSugar;
using Volo.Abp.Domain.Services;
using ConnectionStrings = Volo.Abp.Data.ConnectionStrings;

namespace NS.Module.TenantManagement.Domain;

public class TenantManager : DomainService
{
    /// <summary>
    /// 创建租户（仅构建聚合，不持久化）。
    /// </summary>
    public virtual TenantAggregateRoot CreateAsync(
        string name,
        DbType dbType,
        string defaultConnectionString,
        IEnumerable<(string Name, string Value)>? extraConnectionStrings = null)
    {

        var tenant = new TenantAggregateRoot(name, dbType);

        // 默认连接串（Default）
        tenant.AddOrUpdateConnectionString(ConnectionStrings.DefaultConnectionStringName, defaultConnectionString);

        // 其他命名连接串
        if (extraConnectionStrings != null)
        {
            foreach (var cs in extraConnectionStrings)
            {
                if (string.IsNullOrWhiteSpace(cs.Name) || string.IsNullOrWhiteSpace(cs.Value))
                {
                    continue;
                }

                var nameTrimmed = cs.Name.Trim();

                tenant.AddOrUpdateConnectionString(nameTrimmed, cs.Value);
            }
        }

        return tenant;
    }

    /// <summary>
    /// 更新租户连接串（仅覆盖传入的项，不清空其他项）
    /// </summary>
    public virtual void UpdateConnectionStrings(
        TenantAggregateRoot tenant,
        string? defaultConnectionString,
        IEnumerable<(string Name, string Value)>? extraConnectionStrings = null)
    {
        if (tenant == null) throw new ArgumentNullException(nameof(tenant));

        if (!string.IsNullOrWhiteSpace(defaultConnectionString))
        {
            tenant.AddOrUpdateConnectionString(
                ConnectionStrings.DefaultConnectionStringName,
                defaultConnectionString);
        }

        if (extraConnectionStrings == null) return;

        foreach (var cs in extraConnectionStrings)
        {
            if (string.IsNullOrWhiteSpace(cs.Name) || string.IsNullOrWhiteSpace(cs.Value))
            {
                continue;
            }

            tenant.AddOrUpdateConnectionString(cs.Name.Trim(), cs.Value);
        }
    }
}
