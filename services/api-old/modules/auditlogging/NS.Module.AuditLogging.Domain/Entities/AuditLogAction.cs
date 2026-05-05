using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace NS.Module.AuditLogging.Domain.Entities;

[DisableAuditing]
[SugarTable("audit_log_action")]
[SugarIndex($"index_{nameof(AuditLogId)}", nameof(AuditLogId), OrderByType.Asc)]
[SugarIndex($"index_{nameof(TenantId)}_{nameof(ExecutionTime)}", nameof(TenantId), OrderByType.Asc, nameof(ServiceName), OrderByType.Asc, nameof(MethodName), OrderByType.Asc, nameof(ExecutionTime), OrderByType.Asc)]
public class AuditLogAction: Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    
    public Guid? AuditLogId { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? ServiceName { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? MethodName { get; set; }
    
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? Parameters { get; set; }
    
    public DateTime? ExecutionTime { get; set; }
    
    public int? ExecutionDuration { get; set; }
    
    public AuditLogAction(){}

    public AuditLogAction(Guid auditLogId, AuditLogActionInfo actionInfo)
    {
        AuditLogId = auditLogId;
        ServiceName = actionInfo.ServiceName;
        MethodName = actionInfo.MethodName;
        Parameters = actionInfo.Parameters;
        ExecutionTime = actionInfo.ExecutionTime;
        ExecutionDuration = actionInfo.ExecutionDuration;
        
    }
}

