using NS.Module.AuditLogging.Domain.Entities;

namespace NS.Module.AuditLogging.Domain;

public class EntityChangeWithUsername
{
    public EntityChange EntityChange { get; set; }

    public string UserName { get; set; }
}