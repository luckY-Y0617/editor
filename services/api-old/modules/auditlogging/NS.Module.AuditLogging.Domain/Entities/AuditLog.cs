using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Data;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;
using NS.Module.AuditLogging.Domain.Shared.Consts;

namespace NS.Module.AuditLogging.Domain.Entities;

[DisableAuditing]
[SugarTable("audit_log")]
[SugarIndex($"index_{nameof(ExecutionTime)}", nameof(TenantId), OrderByType.Asc, nameof(ExecutionTime), OrderByType.Asc)]
[SugarIndex($"index_{nameof(ExecutionTime)}_{nameof(UserId)}", nameof(TenantId), OrderByType.Asc, nameof(UserId), OrderByType.Asc, nameof(ExecutionTime), OrderByType.Asc)]
public class AuditLog : AggregateRoot<Guid>, IMultiTenant
{
    [SugarColumn(IsNullable = true)]
    public string? ApplicationName { get; set; }

    public Guid? UserId { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? UserName { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? TenantName { get; protected set; }

    public Guid? ImpersonatorUserId { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? ImpersonatorUserName { get; protected set; }

    public Guid? ImpersonatorTenantId { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? ImpersonatorTenantName { get; protected set; }

    public DateTime? ExecutionTime { get; protected set; }

    public int? ExecutionDuration { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? ClientIpAddress { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? ClientName { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? ClientId { get; set; }

    [SugarColumn(IsNullable = true)]
    public string? CorrelationId { get; set; }

    [SugarColumn(Length = 2000, IsNullable = true)]
    public string? BrowserInfo { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? HttpMethod { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? Url { get; protected set; }

    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? Exceptions { get; protected set; }

    [SugarColumn(IsNullable = true)]
    public string? Comments { get; protected set; }

    public int? HttpStatusCode { get; set; }

    public Guid? TenantId { get; protected set; }

    // 导航属性，默认初始化避免null引用
    [Navigate(NavigateType.OneToMany, nameof(EntityChange.AuditLogId))]
    public List<EntityChange> EntityChanges { get; protected set; }

    [Navigate(NavigateType.OneToMany, nameof(AuditLogAction.AuditLogId))]
    public List<AuditLogAction> Actions { get; protected set; } 

    
    public AuditLog()
    {
        EntityChanges = new List<EntityChange>();
        Actions = new List<AuditLogAction>();
    }

    public AuditLog(
        string? applicationName,
        Guid? tenantId,
        string? tenantName,
        Guid? userId,
        string? userName,
        DateTime executionTime,
        int executionDuration,
        string? clientIpAddress,
        string? clientName,
        string? clientId,
        string? correlationId,
        string? browserInfo,
        string? httpMethod,
        string? url,
        int? httpStatusCode,
        Guid? impersonatorUserId,
        string? impersonatorUserName,
        Guid? impersonatorTenantId,
        string? impersonatorTenantName,
        ExtraPropertyDictionary extraProperties,
        List<EntityChange> entityChanges,
        List<AuditLogAction> actions,
        string? exceptions,
        string? comments)
    {
        ApplicationName = applicationName.Truncate(AuditLogConsts.MaxApplicationNameLength);
        TenantName = tenantName.Truncate(AuditLogConsts.MaxTenantNameLength);
        UserId = userId;
        UserName = userName.Truncate(AuditLogConsts.MaxUserNameLength);
        ExecutionTime = executionTime;
        ExecutionDuration = executionDuration;
        ClientIpAddress = clientIpAddress.Truncate(AuditLogConsts.MaxClientIpAddressLength);
        ClientName = clientName.Truncate(AuditLogConsts.MaxClientNameLength);
        ClientId = clientId.Truncate(AuditLogConsts.MaxClientIdLength);
        CorrelationId = correlationId.Truncate(AuditLogConsts.MaxCorrelationIdLength);
        BrowserInfo = browserInfo.Truncate(AuditLogConsts.MaxBrowserInfoLength);
        HttpMethod = httpMethod.Truncate(AuditLogConsts.MaxHttpMethodLength);
        Url = url.Truncate(AuditLogConsts.MaxUrlLength);
        HttpStatusCode = httpStatusCode;
        ImpersonatorUserId = impersonatorUserId;
        ImpersonatorUserName = impersonatorUserName.Truncate(AuditLogConsts.MaxUserNameLength);
        ImpersonatorTenantId = impersonatorTenantId;
        ImpersonatorTenantName = impersonatorTenantName.Truncate(AuditLogConsts.MaxTenantNameLength);

        ExtraProperties = extraProperties;
        EntityChanges = entityChanges;
        Actions = actions;

        Exceptions = exceptions;
        Comments = comments.Truncate(AuditLogConsts.MaxCommentsLength);
    }

}
