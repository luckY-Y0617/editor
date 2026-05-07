using Northstar.Contracts.Organizations;

namespace Northstar.Application.Organizations;

public interface IOrganizationSettingsCommandService
{
    Task<OrganizationProfileResponse> UpdateProfileAsync(
        Guid organizationId,
        UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken = default);
}
