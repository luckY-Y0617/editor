using Northstar.Domain.Organizations;

namespace Northstar.Application.Organizations;

public interface IOrganizationSettingsRepository
{
    Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> UserCanViewOrganizationAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetActiveOrganizationWorkspaceRolesAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<Organization?> GetOrganizationForUpdateAsync(Guid organizationId, CancellationToken cancellationToken = default);
    Task<bool> OrganizationSlugExistsAsync(
        string slug,
        Guid exceptOrganizationId,
        CancellationToken cancellationToken = default);
    Task<OrganizationProfileReadModel?> GetProfileAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationMemberFlatReadModel>> GetMemberRowsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default);
}
