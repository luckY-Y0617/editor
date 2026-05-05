using System;
using Volo.Abp.Application.Dtos;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class EntityPropertyChangeDto : EntityDto<Guid>
{
    public Guid? EntityChangeId { get; set; }
    
    public string? PropertyName { get; set; }
    
    public string? PropertyTypeFullName { get; set; }
    
    public string? OriginalValue { get; set; }
    
    public string? NewValue { get; set; }
}

