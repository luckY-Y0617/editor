using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IAccessRequestRepository
{
    Task<AccessRequest?> GetForUpdateAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<AccessRequest?> GetAsync(Guid requestId, CancellationToken cancellationToken = default);
    Task<AccessRequest?> GetPendingAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessRequest>> GetByWorkspaceAsync(
        Guid workspaceId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AccessRequest>> GetByResourceAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetWorkspaceManagerUserIdsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(AccessRequest request, CancellationToken cancellationToken = default);
}
