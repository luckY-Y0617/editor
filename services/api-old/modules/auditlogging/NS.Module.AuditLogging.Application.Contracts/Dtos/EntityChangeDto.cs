using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class EntityChangeDto : EntityDto<Guid>
{
    public Guid AuditLogId { get; set; }
    
    public DateTime? ChangeTime { get; set; }
    
    public EntityChangeType? ChangeType { get; set; }
    
    public Guid? EntityTenantId { get; set; }
    
    public string? EntityId { get; set; }
    
    public string? EntityTypeFullName { get; set; }
    
    public List<EntityPropertyChangeDto> PropertyChanges { get; set; } = new();
}

