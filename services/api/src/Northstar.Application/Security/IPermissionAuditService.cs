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

    Task<WorkspaceAuditLogResponse> GetWorkspaceAuditLogAsync(
        Guid workspaceId,
        string? action,
        string? resourceType,
        Guid? resourceId,
        Guid? actorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default);
}
