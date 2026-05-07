using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Organizations;
using Northstar.Domain.Security;

namespace Northstar.Application.Organizations;

public sealed class OrganizationSettingsCommandService : IOrganizationSettingsCommandService
{
    private const int NameMaxLength = 120;
    private const int SlugMaxLength = 80;

    private readonly IOrganizationSettingsRepository _repository;
    private readonly ICurrentUser _currentUser;
    private readonly IPermissionCatalog _permissionCatalog;
    private readonly IUnitOfWork _unitOfWork;

    public OrganizationSettingsCommandService(
        IOrganizationSettingsRepository repository,
        ICurrentUser currentUser,
        IPermissionCatalog permissionCatalog,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _currentUser = currentUser;
        _permissionCatalog = permissionCatalog;
        _unitOfWork = unitOfWork;
    }

    public async Task<OrganizationProfileResponse> UpdateProfileAsync(
        Guid organizationId,
        UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetAuthorizedUserIdAsync(organizationId, cancellationToken);
        var normalized = ValidateRequest(request);

        var organization = await _repository.GetOrganizationForUpdateAsync(organizationId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Organization was not found.");

        if (await _repository.OrganizationSlugExistsAsync(normalized.Slug, organizationId, cancellationToken))
        {
            throw new ApplicationErrorException(ErrorCodes.Conflict, "Organization slug is already in use.");
        }

        organization.UpdateProfile(normalized.Name, normalized.Slug);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var profile = await _repository.GetProfileAsync(organizationId, userId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Organization was not found.");

        return new OrganizationProfileResponse(ToDto(profile));
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

        var roles = await _repository.GetActiveOrganizationWorkspaceRolesAsync(
            organizationId,
            _currentUser.UserId.Value,
            cancellationToken);
        if (!roles.Any(role => _permissionCatalog.RoleHasPermission(role, PermissionActions.OrganizationManageSettings)))
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Owner permission is required to update organization profile.");
        }

        return _currentUser.UserId.Value;
    }

    private static NormalizedOrganizationProfileUpdate ValidateRequest(UpdateOrganizationProfileRequest request)
    {
        var fields = new Dictionary<string, string[]>();
        if (request is null)
        {
            fields["name"] = ["Organization name is required."];
            fields["slug"] = ["Organization slug is required."];
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "Request validation failed.",
                new { fields });
        }

        var name = request.Name?.Trim() ?? string.Empty;
        var slug = NormalizeSlug(request.Slug);

        if (string.IsNullOrWhiteSpace(name))
        {
            fields["name"] = ["Organization name is required."];
        }
        else if (name.Length > NameMaxLength)
        {
            fields["name"] = [$"Organization name must be {NameMaxLength} characters or fewer."];
        }

        if (string.IsNullOrWhiteSpace(slug))
        {
            fields["slug"] = ["Organization slug is required."];
        }
        else if (slug.Length > SlugMaxLength)
        {
            fields["slug"] = [$"Organization slug must be {SlugMaxLength} characters or fewer."];
        }

        if (fields.Count > 0)
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                "Request validation failed.",
                new { fields });
        }

        return new NormalizedOrganizationProfileUpdate(name, slug);
    }

    private static string NormalizeSlug(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        var previousDash = false;
        foreach (var character in value.Trim().ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z' || character is >= '0' and <= '9')
            {
                builder.Append(character);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
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

    private sealed record NormalizedOrganizationProfileUpdate(string Name, string Slug);
}
