using System.Net;
using NS.Framework.SqlSugar;
using NS.Framework.SqlSugar.Abstractions;
using NS.Module.AuditLogging.Domain;
using NS.Module.AuditLogging.Domain.Entities;
using NS.Module.AuditLogging.Domain.Repositories;
using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;

namespace NS.Module.AuditLogging.SqlSugar;

public class SqlSugarCoreAuditLogRepository
    : SqlSugarRepository<SqlSugarDbContext, AuditLog, Guid>, IAuditLogRepository
{
    public SqlSugarCoreAuditLogRepository(ISqlSugarDbContextProvider<SqlSugarDbContext> sqlSugarDbContextProvider)
        : base(sqlSugarDbContextProvider)
    {
    }

    public async Task<bool> InsertAsync(AuditLog insertObj)
    {
        var db = (await GetDbContextAsync()).Client;
        return await db.InsertNav(insertObj)
            .Include(x => x.Actions)
            .ExecuteCommandAsync();
    }

    public async Task<(List<AuditLog> Items, long TotalCount)> GetPagedListAsync(
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
        CancellationToken cancellationToken = default)
    {
        var query = (await GetSugarQueryableAsync())
            .WhereIF(startTime.HasValue, x => x.ExecutionTime >= startTime)
            .WhereIF(endTime.HasValue, x => x.ExecutionTime <= endTime)
            .WhereIF(hasException == true, x => x.Exceptions != null && x.Exceptions != "")
            .WhereIF(hasException == false, x => x.Exceptions == null || x.Exceptions == "")
            .WhereIF(!string.IsNullOrEmpty(httpMethod), x => x.HttpMethod == httpMethod)
            .WhereIF(!string.IsNullOrEmpty(url), x => x.Url != null && x.Url.Contains(url!))
            .WhereIF(userId.HasValue, x => x.UserId == userId)
            .WhereIF(!string.IsNullOrEmpty(userName), x => x.UserName == userName)
            .WhereIF(!string.IsNullOrEmpty(applicationName), x => x.ApplicationName == applicationName)
            .WhereIF(!string.IsNullOrEmpty(clientIpAddress), x => x.ClientIpAddress == clientIpAddress)
            .WhereIF(!string.IsNullOrEmpty(correlationId), x => x.CorrelationId == correlationId)
            .WhereIF(httpStatusCode.HasValue, x => x.HttpStatusCode == (int)httpStatusCode!)
            .WhereIF(maxExecutionDuration > 0, x => x.ExecutionDuration <= maxExecutionDuration)
            .WhereIF(minExecutionDuration > 0, x => x.ExecutionDuration >= minExecutionDuration);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(string.IsNullOrEmpty(sorting) ? $"{nameof(AuditLog.ExecutionTime)} DESC" : sorting)
            .ToPageListAsync(skipCount, maxResultCount, cancellationToken);

        return (items, totalCount);
    }

    public async Task<Dictionary<DateTime, double>> GetAverageExecutionDurationPerDayAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        var result = await (await GetSugarQueryableAsync())
            .Where(x => x.ExecutionTime > startDate && x.ExecutionTime < endDate.AddDays(1))
            .GroupBy(x => x.ExecutionTime!.Value.Date)
            .Select(x => new
            {
                Day = SqlFunc.AggregateMin(x.ExecutionTime),
                AvgDuration = SqlFunc.AggregateAvg(x.ExecutionDuration)
            })
            .OrderBy(x => x.Day)
            .ToListAsync(cancellationToken);

        return result.ToDictionary(x => x.Day!.Value.Date, x => (double)x.AvgDuration);
    }

    public async Task<EntityChange> GetEntityChangeAsync(Guid entityChangeId, CancellationToken cancellationToken = default)
    {
        var db = (await GetDbContextAsync()).Client;
        var entityChange = await db.Queryable<EntityChange>()
            .Where(x => x.Id == entityChangeId)
            .FirstAsync(cancellationToken);

        return entityChange ?? throw new EntityNotFoundException(typeof(EntityChange), entityChangeId);
    }

    public async Task<(List<EntityChange> Items, long TotalCount)> GetEntityChangePagedListAsync(
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
        CancellationToken cancellationToken = default)
    {
        var db = (await GetDbContextAsync()).Client;

        var query = db.Queryable<EntityChange>()
            .WhereIF(auditLogId.HasValue, x => x.AuditLogId == auditLogId)
            .WhereIF(startTime.HasValue, x => x.ChangeTime >= startTime)
            .WhereIF(endTime.HasValue, x => x.ChangeTime <= endTime)
            .WhereIF(changeType.HasValue, x => x.ChangeType == changeType)
            .WhereIF(!string.IsNullOrEmpty(entityId), x => x.EntityId == entityId)
            .WhereIF(!string.IsNullOrEmpty(entityTypeFullName), x => x.EntityTypeFullName.Contains(entityTypeFullName!));

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(string.IsNullOrEmpty(sorting) ? $"{nameof(EntityChange.ChangeTime)} DESC" : sorting)
            .ToPageListAsync(skipCount, maxResultCount, cancellationToken);

        return (items, totalCount);
    }

    public async Task<EntityChangeWithUsername> GetEntityChangeWithUsernameAsync(
        Guid entityChangeId,
        CancellationToken cancellationToken = default)
    {
        var auditLog = await (await GetSugarQueryableAsync())
            .Where(x => x.EntityChanges.Any(e => e.Id == entityChangeId))
            .FirstAsync(cancellationToken);

        if (auditLog == null)
            throw new EntityNotFoundException(typeof(EntityChange), entityChangeId);

        return new EntityChangeWithUsername
        {
            EntityChange = auditLog.EntityChanges.First(x => x.Id == entityChangeId),
            UserName = auditLog.UserName
        };
    }

    public async Task<List<EntityChangeWithUsername>> GetEntityChangesWithUsernameAsync(
        string entityId,
        string entityTypeFullName,
        CancellationToken cancellationToken = default)
    {
        var db = (await GetDbContextAsync()).Client;

        return await db.Queryable<EntityChange>()
            .Where(x => x.EntityId == entityId && x.EntityTypeFullName == entityTypeFullName)
            .LeftJoin<AuditLog>((x, a) => x.AuditLogId == a.Id)
            .Select((x, a) => new EntityChangeWithUsername { EntityChange = x, UserName = a.UserName })
            .OrderByDescending(x => x.EntityChange.ChangeTime)
            .ToListAsync(cancellationToken);
    }
}
