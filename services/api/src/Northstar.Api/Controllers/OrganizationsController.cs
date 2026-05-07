using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Organizations;
using Northstar.Contracts.Organizations;

namespace Northstar.Api.Controllers;

[ApiController]
[Authorize]
[Route("organizations")]
public sealed class OrganizationsController : ControllerBase
{
    private readonly IOrganizationSettingsQueryService _queryService;
    private readonly IOrganizationSettingsCommandService _commandService;

    public OrganizationsController(
        IOrganizationSettingsQueryService queryService,
        IOrganizationSettingsCommandService commandService)
    {
        _queryService = queryService;
        _commandService = commandService;
    }

    [HttpGet("{organizationId:guid}/profile")]
    public async Task<ActionResult<OrganizationProfileResponse>> GetProfile(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await _queryService.GetProfileAsync(organizationId, cancellationToken);
    }

    [HttpPatch("{organizationId:guid}/profile")]
    public async Task<ActionResult<OrganizationProfileResponse>> UpdateProfile(
        Guid organizationId,
        UpdateOrganizationProfileRequest request,
        CancellationToken cancellationToken)
    {
        return await _commandService.UpdateProfileAsync(organizationId, request, cancellationToken);
    }

    [HttpGet("{organizationId:guid}/members")]
    public async Task<ActionResult<OrganizationMembersResponse>> GetMembers(
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        return await _queryService.GetMembersAsync(organizationId, cancellationToken);
    }
}
