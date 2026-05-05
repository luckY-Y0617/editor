using SqlSugar;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.MultiTenancy;

namespace NS.Module.AuditLogging.Domain.Entities;

[SugarTable("audit_entity_property_change")]
[SugarIndex($"index_{nameof(EntityChangeId)}", nameof(EntityChangeId), OrderByType.Asc)]
public class EntityPropertyChange: Entity<Guid>, IMultiTenant
{
    public Guid? TenantId { get; set; }
    
    public Guid? EntityChangeId { get; set; }
    
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? NewValue { get; set; }
    
    [SugarColumn(ColumnDataType = StaticConfig.CodeFirst_BigString, IsNullable = true)]
    public string? OriginalValue { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? PropertyName { get; set; }
    
    [SugarColumn(IsNullable = true)]
    public string? PropertyTypeFullName { get; set; }
    
    public EntityPropertyChange() {}

    public EntityPropertyChange(Guid entityChangeId, EntityPropertyChangeInfo entityPropertyChangeInfo)
    {
        EntityChangeId = entityChangeId;
        NewValue = entityPropertyChangeInfo.NewValue;
        OriginalValue = entityPropertyChangeInfo.OriginalValue;
        PropertyName = entityPropertyChangeInfo.PropertyName;
        PropertyTypeFullName = entityPropertyChangeInfo.PropertyTypeFullName;
    }
}