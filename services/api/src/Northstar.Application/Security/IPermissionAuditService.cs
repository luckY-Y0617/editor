using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPermissionAuditService
{
    Task AddAsync(PermissionAuditEvent auditEvent, CancellationToken cancellationToken = default);

    Task<PermissionAuditResponse> GetAuditAsync(
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default);
}
