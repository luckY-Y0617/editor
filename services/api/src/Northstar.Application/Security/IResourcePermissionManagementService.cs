using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IResourcePermissionManagementService
{
    Task<ResourcePermissionsResponse> GetResourcePermissionsAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);

    Task<ResourcePermissionsResponse> UpdatePolicyAsync(
        string resourceType,
        Guid resourceId,
        UpdateResourcePolicyRequest request,
        CancellationToken cancellationToken = default);

    Task<PermissionGrantDto> CreateGrantAsync(
        string resourceType,
        Guid resourceId,
        CreatePermissionGrantRequest request,
        CancellationToken cancellationToken = default);

    Task<PermissionGrantDto> UpdateGrantAsync(
        string resourceType,
        Guid resourceId,
        Guid grantId,
        UpdatePermissionGrantRequest request,
        CancellationToken cancellationToken = default);

    Task RevokeGrantAsync(
        string resourceType,
        Guid resourceId,
        Guid grantId,
        RevokePermissionGrantRequest? request,
        CancellationToken cancellationToken = default);
}
