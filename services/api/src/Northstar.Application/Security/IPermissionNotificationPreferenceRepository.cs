using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public interface IPermissionNotificationPreferenceRepository
{
    Task<IReadOnlyList<PermissionNotificationPreference>> GetForUserWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<PermissionNotificationPreference?> GetForUpdateAsync(
        Guid userId,
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        PermissionNotificationPreference preference,
        CancellationToken cancellationToken = default);
}
