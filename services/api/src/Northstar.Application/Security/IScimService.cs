using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IScimService
{
    Task<ScimServiceProviderConfigResponse> GetServiceProviderConfigAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<ScimListResponse<ScimSchemaDto>> GetSchemasAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<ScimListResponse<ScimResourceTypeDto>> GetResourceTypesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<ScimListResponse<ScimUserResource>> GetUsersAsync(
        Guid workspaceId,
        string? filter,
        int? startIndex,
        int? count,
        CancellationToken cancellationToken = default);

    Task<ScimUserResource> GetUserAsync(
        Guid workspaceId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<ScimUserResource> CreateUserAsync(
        Guid workspaceId,
        CreateScimUserRequest request,
        CancellationToken cancellationToken = default);

    Task<ScimUserResource> PatchUserAsync(
        Guid workspaceId,
        string userId,
        ScimPatchRequest request,
        CancellationToken cancellationToken = default);

    Task<ScimUserResource> ReplaceUserAsync(
        Guid workspaceId,
        string userId,
        CreateScimUserRequest request,
        CancellationToken cancellationToken = default);

    Task RejectUserProvisioningAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<ScimListResponse<ScimGroupResource>> GetGroupsAsync(
        Guid workspaceId,
        string? filter,
        int? startIndex,
        int? count,
        CancellationToken cancellationToken = default);

    Task<ScimGroupResource> GetGroupAsync(
        Guid workspaceId,
        string groupId,
        CancellationToken cancellationToken = default);

    Task<ScimGroupResource> CreateGroupAsync(
        Guid workspaceId,
        CreateScimGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<ScimGroupResource> PatchGroupAsync(
        Guid workspaceId,
        string groupId,
        ScimPatchRequest request,
        CancellationToken cancellationToken = default);

    Task<ScimGroupResource> ReplaceGroupAsync(
        Guid workspaceId,
        string groupId,
        CreateScimGroupRequest request,
        CancellationToken cancellationToken = default);

    Task RejectGroupProvisioningAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);
}
