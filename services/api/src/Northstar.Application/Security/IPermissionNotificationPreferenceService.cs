using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IPermissionNotificationPreferenceService
{
    Task<PermissionNotificationPreferencesResponse> GetPreferencesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<PermissionNotificationPreferenceDto> UpdatePreferenceAsync(
        UpdatePermissionNotificationPreferenceRequest request,
        CancellationToken cancellationToken = default);
}
