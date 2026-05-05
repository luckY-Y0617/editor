using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NS.Framework.Authorization.AspNetCore;
using NS.Module.Identity.Domain.Shared.Authorization;
using NS.Module.TenantManagement.Application.Contracts;
using NS.Module.TenantManagement.Application.Contracts.Dtos;
using NS.Module.TenantManagement.Domain;
using NS.Module.TenantManagement.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;

namespace NS.Module.TenantManagement.Application;

[ApiController]
[Route("/api/tenant")]
public class TenantService : ApplicationService, ITenantService
{
    private readonly ISqlSugarTenantRepository _tenantRepository;
    private readonly TenantManager _tenantManager;

    public TenantService(
        ISqlSugarTenantRepository tenantRepository,
        TenantManager tenantManager)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
    }

    #region 查询
    
    [HttpGet("{id:guid}")]
    [RequirePermission(SystemPermissions.Tenants.View)]
    public virtual async Task<TenantGetOutputDto> GetAsync([FromRoute]Guid id)
    {
        var tenant = await _tenantRepository.FindAsync(id, includeDetails: true);
        if (tenant == null)
        {
            throw new EntityNotFoundException(typeof(TenantAggregateRoot), id);
        }

        return ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(tenant);
    }

    [HttpGet]
    public virtual async Task<List<TenantGetListOutputDto>> GetListAsync([FromQuery]TenantGetListInputDto input)
    {
        var query = await _tenantRepository.GetQueryableAsync();

        query = query
            .WhereIF(!string.IsNullOrWhiteSpace(input.Name),
                t => t.Name.Contains(input.Name!))
            .WhereIF(input.StartTime.HasValue,
                t => t.CreationTime >= input.StartTime!.Value)
            .WhereIF(input.EndTime.HasValue,
                t => t.CreationTime <= input.EndTime!.Value)
            .OrderBy(t => t.CreationTime, OrderByType.Desc);


        var entities = await query.ToListAsync();

        var items = ObjectMapper.Map<List<TenantAggregateRoot>, List<TenantGetListOutputDto>>(entities);

        return items;
    }

    #endregion

    #region 创建

    [Authorize]
    [HttpPost]
    [RequirePermission(SystemPermissions.Tenants.Manage)]
    public virtual async Task<TenantGetOutputDto> CreateAsync(TenantCreateDto input)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
        {
            throw new BusinessException("ClayMo.TenantManagement:NameIsRequired");
        }

        if (string.IsNullOrWhiteSpace(input.DefaultConnectionString))
        {
            throw new BusinessException("ClayMo.TenantManagement:CreateRequiresConnectionString");
        }

        // DTO → 领域层输入模型
        var extraInputs = input.ConnectionStrings?.Where(x =>
                !string.IsNullOrWhiteSpace(x.Name) &&
                !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => (x.Name.Trim(), x.Value))
            .ToList();

        var tenant = _tenantManager.CreateAsync(
            input.Name,
            input.DbType,
            input.DefaultConnectionString,
            extraInputs);

        await _tenantRepository.InsertAsync(tenant, autoSave: true);

        return ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(tenant);
    }

    #endregion

    #region 更新
    
    [Authorize]
    [HttpPut("{id:guid}")]
    [RequirePermission(SystemPermissions.Tenants.Manage)]
    public virtual async Task<TenantGetOutputDto> UpdateAsync([FromRoute]Guid id, TenantUpdateDto input)
    {
        var tenant = await _tenantRepository.FindAsync(id, includeDetails: true);
        if (tenant == null)
        {
            throw new EntityNotFoundException(typeof(TenantAggregateRoot), id);
        }

        if (!string.IsNullOrWhiteSpace(input.Name) && !string.Equals(tenant.Name, input.Name, StringComparison.Ordinal))
        {
            tenant.SetName(input.Name);
        }

        if (input.DbType != null)
        {
            tenant.SetDbType(input.DbType.Value);
        }

        await _tenantRepository.UpdateAsync(tenant, autoSave: true);

        return ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(tenant);
    }

    #endregion

    #region 删除

    [Authorize]
    [HttpDelete("{id:guid}")]
    [RequirePermission(SystemPermissions.Tenants.Manage)]
    public virtual async Task DeleteAsync(Guid id)
    {
        var tenant = await _tenantRepository.FindAsync(id);
        if (tenant == null)
        {
            return;
        }

        await _tenantRepository.DeleteAsync(tenant);
    }

    #endregion
}

