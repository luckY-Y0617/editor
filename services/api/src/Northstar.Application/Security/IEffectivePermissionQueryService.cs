using Northstar.Contracts.Security;

namespace Northstar.Application.Security;

public interface IEffectivePermissionQueryService
{
    Task<EffectivePermissionResponse> GetEffectivePermissionAsync(
        string? resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default);
}
