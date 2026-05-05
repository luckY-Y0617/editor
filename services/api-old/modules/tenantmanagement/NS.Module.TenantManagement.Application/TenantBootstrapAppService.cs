using System;
using System.Threading;
using System.Threading.Tasks;
using NS.Framework.Core.Extensions;
using NS.Module.TenantManagement.Application.Contracts;
using NS.Module.TenantManagement.Application.Contracts.Dtos;
using NS.Module.TenantManagement.Domain;
using NS.Module.TenantManagement.Domain.Repositories;
using NS.Module.TenantManagement.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using Volo.Abp.Uow;

namespace NS.Module.TenantManagement.Application;

[Authorize]
[ApiController]
[Route("/api/app/tenant/bootstrap")]
public class TenantBootstrapAppService : ApplicationService, ITenantBootstrapAppService
{
    private readonly ISqlSugarTenantRepository _tenantRepository;
    private readonly TenantManager _tenantManager;

    private readonly ICurrentTenant _currentTenant;
    private readonly IUnitOfWorkManager _uowManager;

    private readonly ITenantProvisioningJobRepository _jobRepository;

    public TenantBootstrapAppService(
        ISqlSugarTenantRepository tenantRepository,
        TenantManager tenantManager,
        ICurrentTenant currentTenant,
        IUnitOfWorkManager uowManager,
        ITenantProvisioningJobRepository jobRepository)
    {
        _tenantRepository = tenantRepository;
        _tenantManager = tenantManager;
        _currentTenant = currentTenant;
        _uowManager = uowManager;
        _jobRepository = jobRepository;
    }

    /// <summary>
    /// 创建租户 + 入队初始化
    /// </summary>
    [HttpPost("create-and-bootstrap")]
    public virtual async Task<TenantBootstrapResultDto> CreateAndBootstrapAsync(
        TenantCreateDto input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.Name))
            throw new BusinessException("ClayMo.TenantManagement:NameIsRequired");

        if (string.IsNullOrWhiteSpace(input.DefaultConnectionString))
            throw new BusinessException("ClayMo.TenantManagement:CreateRequiresConnectionString");
        
        if(await _tenantRepository.IsDuplicatedAsync(input.Name))
            throw new BusinessException("ClayMo.TenantManagement:DuplicatedTenantName");

        var extraInputs = input.ConnectionStrings?.Where(x =>
                !string.IsNullOrWhiteSpace(x.Name) &&
                !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => (x.Name.Trim(), x.Value))
            .ToList();
        
        TenantAggregateRoot tenant;

        using (var uow = _uowManager.Begin(requiresNew: true, isTransactional: false))
        using (_currentTenant.Change(null)) 
        {
            tenant = _tenantManager.CreateAsync(
                input.Name,
                input.DbType,
                input.DefaultConnectionString,
                extraInputs);

            tenant.MarkProvisioning();

            await _tenantRepository.InsertWithConnStrAsync(tenant);

            await _jobRepository.EnqueueAsync(tenant.Id, cancellationToken);

            await uow.CompleteAsync(cancellationToken);
        }

        var reloaded = await _tenantRepository.FindAsync(tenant.Id, includeDetails: true, cancellationToken);

        return new TenantBootstrapResultDto
        {
            TenantId = tenant.Id,
            Succeeded = true, // 表示“已受理并入队”，不是表示“初始化已完成”
            State = reloaded == null ? MapState(tenant) : MapState(reloaded),
            Tenant = reloaded == null ? null : ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(reloaded)
        };
    }

    [HttpPost("{tenantId:guid}")]
    public virtual async Task<TenantBootstrapResultDto> BootstrapAsync(
        [FromRoute] Guid tenantId,
        [FromBody] TenantBootstrapOptionsDto? options = null,
        CancellationToken ct = default)
    {
        TenantAggregateRoot tenant;

        using (_currentTenant.Change(null))
        {
            tenant = await _tenantRepository.FindAsync(tenantId, includeDetails: true, ct)
                     ?? throw new EntityNotFoundException(typeof(TenantAggregateRoot), tenantId);

            // 已 Ready：幂等返回
            if (tenant.ProvisioningState == TenantProvisioningState.Ready)
            {
                return new TenantBootstrapResultDto
                {
                    TenantId = tenantId,
                    Succeeded = true,
                    State = MapState(tenant),
                    Tenant = ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(tenant)
                };
            }

            // 正在 Provisioning：不重复入队（也可选择允许“重置 job”）
            if (tenant.ProvisioningState == TenantProvisioningState.Provisioning)
            {
                return new TenantBootstrapResultDto
                {
                    TenantId = tenantId,
                    Succeeded = true,
                    State = MapState(tenant),
                    Tenant = ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(tenant)
                };
            }

            // Failed / NotReady：重试入队
            tenant.MarkProvisioning();
            await _tenantRepository.UpdateAsync(tenant, autoSave: true, ct);

            await _jobRepository.EnqueueAsync(tenantId, ct);
        }

        // 回填最新
        var reloaded = await _tenantRepository.FindAsync(tenantId, includeDetails: true, ct);

        return new TenantBootstrapResultDto
        {
            TenantId = tenantId,
            Succeeded = true,
            State = reloaded == null ? MapState(tenant) : MapState(reloaded),
            Tenant = reloaded == null ? null : ObjectMapper.Map<TenantAggregateRoot, TenantGetOutputDto>(reloaded)
        };
    }

    /// <summary>
    /// 查询初始化状态：读取 tenant 的 ProvisioningState / LastError。
    /// 如需展示 job 维度信息，可扩展 DTO 并在此查询 job 表。
    /// </summary>
    [HttpGet("{tenantId:guid}/state")]
    public virtual async Task<TenantBootstrapStateDto> GetBootstrapStateAsync([FromRoute] Guid tenantId, CancellationToken ct = default)
    {
        using (var uow = _uowManager.Begin(requiresNew: true, isTransactional: false))
        using (_currentTenant.Change(null))
        {
            var tenant = await _tenantRepository.FindAsync(tenantId, includeDetails: false, ct)
                         ?? throw new EntityNotFoundException(typeof(TenantAggregateRoot), tenantId);

            await uow.CompleteAsync(ct);
            return MapState(tenant);
        }
    }

    private TenantBootstrapStateDto MapState(TenantAggregateRoot tenant)
    {
        return new TenantBootstrapStateDto
        {
            State = tenant.ProvisioningState.ToContractString(),
            ProvisionedAtUtc = tenant.ProvisionedAtUtc,
            LastError = tenant.LastProvisioningError
        };
    }
}
