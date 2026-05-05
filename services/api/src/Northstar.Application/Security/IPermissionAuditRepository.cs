using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPermissionAuditRepository
{
    Task AddAsync(PermissionAuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PermissionAuditEvent>> GetAsync(
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default);
}
