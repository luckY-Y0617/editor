using System.Collections.Generic;

namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class AuditLogDetailDto : AuditLogDto
{
    public string? Exceptions { get; set; }
    
    public List<AuditLogActionDto> Actions { get; set; } = new();
    
    public List<EntityChangeDto> EntityChanges { get; set; } = new();
}

