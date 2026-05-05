using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using NS.Module.AuditLogging.Application.Contracts;
using NS.Module.AuditLogging.Application.Contracts.Dtos;
using NS.Module.AuditLogging.Domain;
using NS.Module.AuditLogging.Domain.Entities;
using NS.Module.AuditLogging.Domain.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NS.Module.AuditLogging.Application;

[Authorize]
[ApiController]
[Route("/api/app/audit-log")]
public class AuditLogAppService : ApplicationService, IAuditLogAppService
{
    private readonly IAuditLogRepository _repository;

    public AuditLogAppService(IAuditLogRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<PagedResultDto<AuditLogDto>> GetListAsync([FromQuery] GetAuditLogListInput input)
    {
        var (items, count) = await _repository.GetPagedListAsync(
            input.Sorting,
            input.MaxResultCount,
            input.SkipCount,
            input.StartTime,
            input.EndTime,
            input.HttpMethod,
            input.Url,
            input.UserId,
            input.UserName,
            input.ApplicationName,
            input.ClientIpAddress,
            input.CorrelationId,
            input.MaxExecutionDuration,
            input.MinExecutionDuration,
            input.HasException,
            input.HttpStatusCode,
            input.IncludeDetails);

        return new PagedResultDto<AuditLogDto>(count, ObjectMapper.Map<List<AuditLog>, List<AuditLogDto>>(items));
    }

    [HttpGet("{id:guid}")]
    public async Task<AuditLogDetailDto> GetAsync([FromRoute] Guid id)
    {
        var auditLog = await _repository.GetAsync(id, includeDetails: true);
        return ObjectMapper.Map<AuditLog, AuditLogDetailDto>(auditLog);
    }

    [HttpGet("entity-changes")]
    public async Task<PagedResultDto<EntityChangeDto>> GetEntityChangeListAsync([FromQuery] GetEntityChangeListInput input)
    {
        var (items, count) = await _repository.GetEntityChangePagedListAsync(
            input.Sorting,
            input.MaxResultCount,
            input.SkipCount,
            input.AuditLogId,
            input.StartTime,
            input.EndTime,
            input.ChangeType,
            input.EntityId,
            input.EntityTypeFullName,
            input.IncludeDetails);

        return new PagedResultDto<EntityChangeDto>(count, ObjectMapper.Map<List<EntityChange>, List<EntityChangeDto>>(items));
    }

    [HttpGet("entity-changes/{entityChangeId:guid}")]
    public async Task<EntityChangeDto> GetEntityChangeAsync([FromRoute] Guid entityChangeId)
    {
        var entityChange = await _repository.GetEntityChangeAsync(entityChangeId);
        return ObjectMapper.Map<EntityChange, EntityChangeDto>(entityChange);
    }

    [HttpGet("entity-changes-with-username")]
    public async Task<List<EntityChangeWithUsernameDto>> GetEntityChangesWithUsernameAsync(
        [FromQuery] string entityId,
        [FromQuery] string entityTypeFullName)
    {
        var list = await _repository.GetEntityChangesWithUsernameAsync(entityId, entityTypeFullName);
        return ObjectMapper.Map<List<EntityChangeWithUsername>, List<EntityChangeWithUsernameDto>>(list);
    }

    [HttpGet("entity-change-with-username/{entityChangeId:guid}")]
    public async Task<EntityChangeWithUsernameDto> GetEntityChangeWithUsernameAsync([FromRoute] Guid entityChangeId)
    {
        var result = await _repository.GetEntityChangeWithUsernameAsync(entityChangeId);
        return ObjectMapper.Map<EntityChangeWithUsername, EntityChangeWithUsernameDto>(result);
    }

    [HttpGet("statistics/average-execution-duration")]
    public async Task<AuditLogStatisticsDto> GetAverageExecutionDurationPerDayAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate)
    {
        var statistics = await _repository.GetAverageExecutionDurationPerDayAsync(startDate, endDate);
        return new AuditLogStatisticsDto { AverageExecutionDurationPerDay = statistics };
    }

    [HttpGet("recent-activities")]
    public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync([FromQuery] int limit = 10)
    {
        var (items, _) = await _repository.GetPagedListAsync(
            sorting: $"{nameof(AuditLog.ExecutionTime)} DESC",
            maxResultCount: limit);

        return items.Select(log => new RecentActivityDto
        {
            UserName = log.UserName ?? "system",
            HttpMethod = log.HttpMethod,
            Url = log.Url,
            HttpStatusCode = log.HttpStatusCode,
            HasException = !string.IsNullOrEmpty(log.Exceptions),
            ExecutionTime = log.ExecutionTime,
            ExecutionDuration = log.ExecutionDuration,
            Description = GenerateDescription(log)
        }).ToList();
    }

    private static string GenerateDescription(AuditLog log)
    {
        var url = log.Url ?? "";
        var method = log.HttpMethod ?? "GET";

        // 特殊路径
        if (url.Contains("/login", StringComparison.OrdinalIgnoreCase)) return "用户登录";
        if (url.Contains("/logout", StringComparison.OrdinalIgnoreCase)) return "用户登出";

        // 资源映射
        var resourceMap = new (string Key, string Name)[]
        {
            ("/users", "用户"), ("/roles", "角色"), ("/tenants", "租户"),
            ("/permissions", "权限"), ("/teams", "团队"), ("/workspace", "工作区"),
            ("/knowledge", "知识库"), ("/goal", "目标")
        };

        foreach (var (key, name) in resourceMap)
        {
            if (url.Contains(key, StringComparison.OrdinalIgnoreCase))
            {
                var action = method switch
                {
                    "POST" => "创建",
                    "PUT" or "PATCH" => "更新",
                    "DELETE" => "删除",
                    _ => "查看"
                };
                return $"{action}{name}";
            }
        }

        var shortUrl = url.Length > 30 ? url[..30] + "..." : url;
        return $"{method} {shortUrl}";
    }
}
