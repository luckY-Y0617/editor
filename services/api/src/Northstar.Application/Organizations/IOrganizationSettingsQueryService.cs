using Northstar.Contracts.Organizations;

namespace Northstar.Application.Organizations;

public interface IOrganizationSettingsQueryService
{
    Task<OrganizationProfileResponse> GetProfileAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<OrganizationMembersResponse> GetMembersAsync(Guid organizationId, CancellationToken cancellationToken = default);
}
