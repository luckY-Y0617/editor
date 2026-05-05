namespace NS.Module.AuditLogging.Application.Contracts.Dtos;

public class EntityChangeWithUsernameDto
{
    public EntityChangeDto EntityChange { get; set; } = null!;
    
    public string? UserName { get; set; }
}

