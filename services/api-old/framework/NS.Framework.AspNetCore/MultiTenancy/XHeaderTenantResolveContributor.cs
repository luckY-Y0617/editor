using Microsoft.AspNetCore.Http;
using Volo.Abp.AspNetCore.MultiTenancy;
using Volo.Abp.MultiTenancy;

namespace NS.Framework.AspNetCore.MultiTenancy;

public class XHeaderTenantResolveContributor : HttpTenantResolveContributorBase
{
    public const string HeaderName = "X-Tenant-Id";

    public override string Name => "XHeaderTenant";

    protected override Task<string?> GetTenantIdOrNameFromHttpContextOrNullAsync(
        ITenantResolveContext context,
        HttpContext httpContext)
    {
        // 尝试从 Header 中获取租户ID
        if (httpContext.Request.Headers.TryGetValue(HeaderName, out var values))
        {
            var tenantId = values.FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return Task.FromResult<string?>(tenantId);
            }
        }

        return Task.FromResult<string?>(null);
    }
}