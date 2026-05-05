using System.Net;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.AuditLogging.Domain.Entities;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;

namespace NS.Module.AuditLogging.Domain.Repositories;

public interface IAuditLogRepository : ISqlSugarRepository<AuditLog, Guid>, ITransientDependency
{
    Task<bool> InsertAsync(AuditLog auditLog);

    Task<(List<AuditLog> Items, long TotalCount)> GetPagedListAsync(
        string? sorting = null,
        int maxResultCount = 50,
        int skipCount = 0,
        DateTime? startTime = null,
        DateTime? endTime = null,
        string? httpMethod = null,
        string? url = null,
        Guid? userId = null,
        string? userName = null,
        string? applicationName = null,
        string? clientIpAddress = null,
        string? correlationId = null,
        int? maxExecutionDuration = null,
        int? minExecutionDuration = null,
        bool? hasException = null,
        HttpStatusCode? httpStatusCode = null,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<Dictionary<DateTime, double>> GetAverageExecutionDurationPerDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    Task<EntityChange> GetEntityChangeAsync(Guid entityChangeId, CancellationToken cancellationToken = default);

    Task<(List<EntityChange> Items, long TotalCount)> GetEntityChangePagedListAsync(
        string? sorting = null,
        int maxResultCount = 50,
        int skipCount = 0,
        Guid? auditLogId = null,
        DateTime? startTime = null,
        DateTime? endTime = null,
        EntityChangeType? changeType = null,
        string? entityId = null,
        string? entityTypeFullName = null,
        bool includeDetails = false,
        CancellationToken cancellationToken = default);

    Task<EntityChangeWithUsername> GetEntityChangeWithUsernameAsync(Guid entityChangeId, CancellationToken cancellationToken = default);

    Task<List<EntityChangeWithUsername>> GetEntityChangesWithUsernameAsync(
        string entityId,
        string entityTypeFullName,
        CancellationToken cancellationToken = default);
}
