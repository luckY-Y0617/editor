using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Organizations;

namespace Northstar.Application.Organizations;

public sealed class OrganizationSettingsQueryService : IOrganizationSettingsQueryService
{
    private readonly IOrganizationSettingsRepository _repository;
    private readonly ICurrentUser _currentUser;

    public OrganizationSettingsQueryService(
        IOrganizationSettingsRepository repository,
        ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    public async Task<OrganizationProfileResponse> GetProfileAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetAuthorizedUserIdAsync(organizationId, cancellationToken);
        var profile = await _repository.GetProfileAsync(organizationId, userId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Organization was not found.");

        return new OrganizationProfileResponse(ToDto(profile));
    }

    public async Task<OrganizationMembersResponse> GetMembersAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        await GetAuthorizedUserIdAsync(organizationId, cancellationToken);

        var rows = await _repository.GetMemberRowsAsync(organizationId, cancellationToken);
        var members = rows
            .GroupBy(row => new { row.UserId, row.Email, row.DisplayName })
            .OrderBy(group => group.Key.DisplayName)
            .ThenBy(group => group.Key.Email)
            .Select(group =>
            {
                var statuses = group.Select(row => row.Status).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
                var status = statuses.Contains("active", StringComparer.OrdinalIgnoreCase)
                    ? "active"
                    : statuses.FirstOrDefault() ?? "unknown";

                return new OrganizationMemberDto(
                    group.Key.UserId.ToString(),
                    group.Key.Email,
                    group.Key.DisplayName,
                    status,
                    group
                        .OrderBy(row => row.WorkspaceName)
                        .Select(row => new OrganizationMemberWorkspaceDto(
                            row.WorkspaceId,
                            row.WorkspaceName,
                            row.Role,
                            row.Status,
                            row.JoinedAt))
                        .ToArray());
            })
            .ToArray();

        return new OrganizationMembersResponse(members);
    }

    private async Task<Guid> GetAuthorizedUserIdAsync(Guid organizationId, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        if (!await _repository.OrganizationExistsAsync(organizationId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Organization was not found.");
        }

        if (!await _repository.UserCanViewOrganizationAsync(organizationId, _currentUser.UserId.Value, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "You do not have access to this organization.");
        }

        return _currentUser.UserId.Value;
    }

    private static OrganizationProfileDto ToDto(OrganizationProfileReadModel profile)
    {
        return new OrganizationProfileDto(
            profile.Id.ToString(),
            profile.Name,
            profile.Slug,
            profile.Status,
            profile.Workspaces
                .OrderBy(workspace => workspace.Name)
                .Select(workspace => new OrganizationWorkspaceDto(
                    workspace.Id.ToString(),
                    workspace.Name,
                    workspace.Slug,
                    workspace.CurrentSpaceId?.ToString() ?? string.Empty,
                    workspace.CurrentUserRole,
                    workspace.CreatedAt))
                .ToArray(),
            profile.CreatedAt,
            profile.UpdatedAt);
    }
}
