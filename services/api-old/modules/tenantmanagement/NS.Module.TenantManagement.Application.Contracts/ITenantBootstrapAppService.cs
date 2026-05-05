using System;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.TenantManagement.Application.Contracts.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.TenantManagement.Application.Contracts;

public interface ITenantBootstrapAppService : IApplicationService
{
    Task<TenantBootstrapResultDto> CreateAndBootstrapAsync(
        TenantCreateDto input,
        CancellationToken cancellationToken = default);

    Task<TenantBootstrapResultDto> BootstrapAsync(
        Guid tenantId,
        TenantBootstrapOptionsDto? options = null,
        CancellationToken cancellationToken = default);

    Task<TenantBootstrapStateDto> GetBootstrapStateAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default);
}