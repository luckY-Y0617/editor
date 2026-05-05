using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IResourcePermissionRepository
{
    Task<ResourceAccessPolicy?> GetPolicyAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<ResourceAccessPolicy?> GetPolicyForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceAccessPolicy>> GetPoliciesForResourcesAsync(
        Guid workspaceId,
        string resourceType,
        IReadOnlyCollection<Guid> resourceIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceAccessGrant>> GetUserGrantsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceAccessGrant>> GetGrantsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<ResourceAccessGrant?> GetActiveUserGrantAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<ResourceAccessGrant?> GetActiveSubjectGrantAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        IReadOnlyCollection<Guid> groupIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceAccessGrant>> GetActiveGroupGrantsForResourcesAsync(
        Guid workspaceId,
        string resourceType,
        IReadOnlyCollection<Guid> resourceIds,
        IReadOnlyCollection<Guid> groupIds,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<ResourceAccessGrant?> GetGrantForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid grantId,
        CancellationToken cancellationToken = default);

    Task<ResourceAccessGrant?> GetUserGrantForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<ResourceAccessGrant?> GetSubjectGrantForUpdateAsync(
        Guid workspaceId,
        string resourceType,
        Guid resourceId,
        string subjectType,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ResourceAccessGrant>> GetActiveUserGrantsForResourcesAsync(
        Guid workspaceId,
        string resourceType,
        IReadOnlyCollection<Guid> resourceIds,
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task AddPolicyAsync(ResourceAccessPolicy policy, CancellationToken cancellationToken = default);
    Task AddGrantAsync(ResourceAccessGrant grant, CancellationToken cancellationToken = default);
}
