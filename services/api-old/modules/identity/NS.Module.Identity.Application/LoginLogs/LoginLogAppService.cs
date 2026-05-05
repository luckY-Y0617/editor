using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NS.Module.Identity.Application.Contracts.identities.Dtos;
using NS.Module.Identity.Domain.Identities;
using NS.Module.Identity.Domain.Identities.Repositories;
using NS.Module.Identity.Domain.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace NS.Module.Identity.Application.LoginLogs;

[ApiController]
[Route("/api/app/login-logs")]
public sealed class LoginLogAppService : ApplicationService
{
    private readonly ILoginLogRepository _loginLogRepository;
    private readonly ICurrentTenant _currentTenant;

    public LoginLogAppService(
        ILoginLogRepository loginLogRepository,
        ICurrentTenant currentTenant)
    {
        _loginLogRepository = loginLogRepository;
        _currentTenant = currentTenant;
    }

    [HttpGet]
    [Authorize]
    public async Task<PagedResultDto<LoginLogDto>> GetListAsync(
        [FromQuery] LoginLogGetListInputDto input,
        CancellationToken ct = default)
    {
        return await GetPagedListAsync(
            input,
            userId: ParseUserId(input.UserId),
            ct);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<LoginLogDto> GetAsync([FromRoute] Guid id, CancellationToken ct = default)
    {
        var log = await _loginLogRepository.FindAsync(id, includeDetails: false, ct)
                  ?? throw new EntityNotFoundException(typeof(LoginLog), id);

        EnsureTenantScope(log);
        return ObjectMapper.Map<LoginLog, LoginLogDto>(log);
    }

    [HttpGet("user/{userId:guid}")]
    [Authorize]
    public async Task<PagedResultDto<LoginLogDto>> GetUserLogsAsync(
        [FromRoute] Guid userId,
        [FromQuery] LoginLogGetListInputDto input,
        CancellationToken ct = default)
    {
        return await GetPagedListAsync(input, userId, ct);
    }

    private async Task<PagedResultDto<LoginLogDto>> GetPagedListAsync(
        LoginLogGetListInputDto input,
        Guid? userId,
        CancellationToken ct)
    {
        var tenantId = _currentTenant.Id;
        var loginStatus = ParseLoginStatus(input.LoginStatus);
        var startTime = ParseUtcTime(input.StartTime);
        var endTime = ParseUtcTime(input.EndTime);

        if (startTime.HasValue && endTime.HasValue && startTime > endTime)
        {
            throw new BusinessException("Identity:InvalidTimeRange", "开始时间不能大于结束时间");
        }

        var sorting = string.IsNullOrWhiteSpace(input.Sorting)
            ? $"{nameof(LoginLog.LoginTime)} desc"
            : input.Sorting;

        var pageIndex = input.MaxResultCount <= 0
            ? 1
            : (input.SkipCount / input.MaxResultCount) + 1;

        RefAsync<int> totalCount = 0;

        var query = await _loginLogRepository.GetQueryableAsync();

        if (tenantId.HasValue)
        {
            query = query.Where(log => log.TenantId == tenantId);
        }

        if (!string.IsNullOrWhiteSpace(input.UserName))
        {
            query = query.Where(log => log.UserName != null && log.UserName.Contains(input.UserName));
        }

        if (loginStatus.HasValue)
        {
            query = query.Where(log => log.LoginStatus == loginStatus.Value);
        }

        if (startTime.HasValue)
        {
            query = query.Where(log => log.LoginTime >= startTime.Value);
        }

        if (endTime.HasValue)
        {
            query = query.Where(log => log.LoginTime <= endTime.Value);
        }

        var logs = await query
            .OrderBy(sorting)
            .ToPageListAsync(pageIndex, input.MaxResultCount, totalCount, ct);

        var items = ObjectMapper.Map<List<LoginLog>, List<LoginLogDto>>(logs);
        return new PagedResultDto<LoginLogDto>(totalCount, items);
    }

    private void EnsureTenantScope(LoginLog log)
    {
        var tenantId = _currentTenant.Id;
        if (!tenantId.HasValue)
        {
            return;
        }

        if (log.TenantId != tenantId)
        {
            throw new BusinessException("Identity:LoginLogNotFound", "登录记录不存在");
        }
    }

    private static Guid? ParseUserId(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Guid.TryParse(value, out var userId))
        {
            return userId;
        }

        throw new BusinessException("Identity:InvalidUserId", "用户ID格式不正确");
    }

    private static LoginStatusEnum? ParseLoginStatus(int? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value switch
        {
            0 => LoginStatusEnum.Failed,
            1 => LoginStatusEnum.Success,
            _ => throw new BusinessException("Identity:InvalidLoginStatus", "登录状态不合法")
        };
    }

    private static DateTime? ParseUtcTime(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out var time))
        {
            return time;
        }

        throw new BusinessException("Identity:InvalidTime", "时间格式不正确");
    }

    private static string FormatTime(DateTime time)
        => time.ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture);

    private static string? FormatTime(DateTime? time)
        => time.HasValue ? FormatTime(time.Value) : null;
}

