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

    Task<PermissionAuditPage> SearchWorkspaceAsync(
        PermissionAuditQuery query,
        CancellationToken cancellationToken = default);
}

public sealed record PermissionAuditQuery(
    Guid WorkspaceId,
    string? Action,
    string? ResourceType,
    Guid? ResourceId,
    Guid? ActorId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Offset,
    int Limit);

public sealed record PermissionAuditPage(
    IReadOnlyList<PermissionAuditRow> Events,
    int TotalCount);

public sealed record PermissionAuditRow(
    PermissionAuditEvent Event,
    string? ActorName,
    string? ActorEmail);
