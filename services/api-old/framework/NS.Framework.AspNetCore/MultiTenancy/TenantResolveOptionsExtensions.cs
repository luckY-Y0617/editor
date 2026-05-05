using Volo.Abp.MultiTenancy;

namespace NS.Framework.AspNetCore.MultiTenancy;

public static class TenantResolveOptionsExtensions
{
    /// <summary>
    /// 向多租户解析链中加入基于 Header 的租户解析器。
    /// 默认解析 Header: X-Tenant-Id
    /// </summary>
    public static void AddXHeaderTenantResolver(this AbpTenantResolveOptions options)
    {
        // 插入优先级最高的位置
        options.TenantResolvers.Insert(0, new XHeaderTenantResolveContributor());
    }
}