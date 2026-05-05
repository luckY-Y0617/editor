using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using NS.Module.AuditLogging.Application.Contracts.Dtos;

namespace NS.Module.AuditLogging.Application.Contracts;

public interface IAuditLogAppService : IApplicationService
{
    /// <summary>
    /// 获取审计日志列表
    /// </summary>
    Task<PagedResultDto<AuditLogDto>> GetListAsync(GetAuditLogListInput input);
    
    /// <summary>
    /// 获取审计日志详情
    /// </summary>
    Task<AuditLogDetailDto> GetAsync(Guid id);
    
    /// <summary>
    /// 获取实体变更列表
    /// </summary>
    Task<PagedResultDto<EntityChangeDto>> GetEntityChangeListAsync(GetEntityChangeListInput input);
    
    /// <summary>
    /// 获取实体变更详情
    /// </summary>
    Task<EntityChangeDto> GetEntityChangeAsync(Guid entityChangeId);
    
    /// <summary>
    /// 获取实体变更列表（包含用户名�?
    /// </summary>
    Task<List<EntityChangeWithUsernameDto>> GetEntityChangesWithUsernameAsync(string entityId, string entityTypeFullName);
    
    /// <summary>
    /// 获取实体变更详情（包含用户名�?
    /// </summary>
    Task<EntityChangeWithUsernameDto> GetEntityChangeWithUsernameAsync(Guid entityChangeId);
    
    /// <summary>
    /// 获取平均执行时长统计（按天）
    /// </summary>
    Task<AuditLogStatisticsDto> GetAverageExecutionDurationPerDayAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取最近操作活动（仪表盘用）
    /// </summary>
    Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int limit = 10);
}

