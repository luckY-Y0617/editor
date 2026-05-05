using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Module.TenantManagement.Application.Contracts.Dtos;
using Volo.Abp.Application.Services;

namespace NS.Module.TenantManagement.Application.Contracts
{
    public interface ITenantService : IApplicationService
    {
        Task<TenantGetOutputDto> GetAsync(Guid id);

        Task<List<TenantGetListOutputDto>> GetListAsync(TenantGetListInputDto input);

        Task<TenantGetOutputDto> CreateAsync(TenantCreateDto input);

        Task<TenantGetOutputDto> UpdateAsync(Guid id, TenantUpdateDto input);

        Task DeleteAsync(Guid id);
    }
}