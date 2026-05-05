using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace NS.Module.AuditLogging.Domain.Entities;

[SugarTable("auidt_entity_change")]
[SugarIndex($"index_{nameof(AuditLogId)}", nameof(AuditLogId), OrderByType.Asc)]
[SugarIndex($"index_{nameof(TenantId)}_{nameof(EntityId)}", nameof(TenantId), OrderByType.Asc, nameof(EntityTypeFullName), OrderByType.Asc, nameof(EntityId), OrderByType.Asc)]
public class EntityChange: Entity<Guid>, IMultiTenant
{
    public Guid AuditLogId { get; set; }
    
    public Guid? TenantId { get; }
    
    public DateTime? ChangeTime { get; set; }
    
    public EntityChangeType? ChangeType { get; set; }
    
    public Guid? EntityTenantId { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? EntityId { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? EntityTypeFullName { get; set; }
    
    [SugarColumn(IsIgnore = true)]
    public List<EntityPropertyChange> PropertyChanges { get; set; } = new();
    
    public EntityChange() {}

    public EntityChange(Guid auditLogId, EntityChangeInfo entityChangeInfo)
    {
        AuditLogId = auditLogId;
        ChangeTime = entityChangeInfo.ChangeTime;
        ChangeType = entityChangeInfo.ChangeType;
        EntityId = entityChangeInfo.EntityId;
        EntityTypeFullName = entityChangeInfo.EntityTypeFullName;
        PropertyChanges = entityChangeInfo.PropertyChanges
            .Select(x => new EntityPropertyChange())
            .ToList();
    }
}