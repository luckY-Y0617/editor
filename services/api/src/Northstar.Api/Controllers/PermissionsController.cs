using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Security;
using Northstar.Contracts.Security;

namespace Northstar.Api.Controllers;

[Authorize]
[ApiController]
[Route("permissions")]
public sealed class PermissionsController : ControllerBase
{
    private readonly IEffectivePermissionQueryService _permissionQueryService;
    private readonly IResourcePermissionManagementService _permissionManagementService;
    private readonly IPermissionAuditService _permissionAuditService;
    private readonly IAccessRequestService _accessRequestService;
    private readonly IShareLinkService _shareLinkService;
    private readonly IEmailInviteService _emailInviteService;

    public PermissionsController(
        IEffectivePermissionQueryService permissionQueryService,
        IResourcePermissionManagementService permissionManagementService,
        IPermissionAuditService permissionAuditService,
        IAccessRequestService accessRequestService,
        IShareLinkService shareLinkService,
        IEmailInviteService emailInviteService)
    {
        _permissionQueryService = permissionQueryService;
        _permissionManagementService = permissionManagementService;
        _permissionAuditService = permissionAuditService;
        _accessRequestService = accessRequestService;
        _shareLinkService = shareLinkService;
        _emailInviteService = emailInviteService;
    }

    [HttpGet("effective")]
    [ProducesResponseType(typeof(EffectivePermissionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EffectivePermissionResponse>> GetEffectivePermission(
        [FromQuery] string? resourceType,
        [FromQuery] Guid resourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _permissionQueryService.GetEffectivePermissionAsync(
            resourceType,
            resourceId,
            cancellationToken));
    }

    [HttpGet("resources/{resourceType}/{resourceId:guid}")]
    [ProducesResponseType(typeof(ResourcePermissionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResourcePermissionsResponse>> GetResourcePermissions(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _permissionManagementService.GetResourcePermissionsAsync(
            resourceType,
            resourceId,
            cancellationToken));
    }

    [HttpPatch("resources/{resourceType}/{resourceId:guid}/policy")]
    [ProducesResponseType(typeof(ResourcePermissionsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResourcePermissionsResponse>> UpdatePolicy(
        string resourceType,
        Guid resourceId,
        UpdateResourcePolicyRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _permissionManagementService.UpdatePolicyAsync(
            resourceType,
            resourceId,
            request,
            cancellationToken));
    }

    [HttpPost("resources/{resourceType}/{resourceId:guid}/grants")]
    [ProducesResponseType(typeof(PermissionGrantDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionGrantDto>> CreateGrant(
        string resourceType,
        Guid resourceId,
        CreatePermissionGrantRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _permissionManagementService.CreateGrantAsync(
            resourceType,
            resourceId,
            request,
            cancellationToken));
    }

    [HttpPatch("resources/{resourceType}/{resourceId:guid}/grants/{grantId:guid}")]
    [ProducesResponseType(typeof(PermissionGrantDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionGrantDto>> UpdateGrant(
        string resourceType,
        Guid resourceId,
        Guid grantId,
        UpdatePermissionGrantRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _permissionManagementService.UpdateGrantAsync(
            resourceType,
            resourceId,
            grantId,
            request,
            cancellationToken));
    }

    [HttpDelete("resources/{resourceType}/{resourceId:guid}/grants/{grantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeGrant(
        string resourceType,
        Guid resourceId,
        Guid grantId,
        RevokePermissionGrantRequest? request,
        CancellationToken cancellationToken)
    {
        await _permissionManagementService.RevokeGrantAsync(
            resourceType,
            resourceId,
            grantId,
            request,
            cancellationToken);
        return NoContent();
    }

    [HttpGet("resources/{resourceType}/{resourceId:guid}/share-links")]
    [ProducesResponseType(typeof(ShareLinksResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShareLinksResponse>> GetShareLinks(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.GetShareLinksAsync(resourceType, resourceId, cancellationToken));
    }

    [HttpPost("resources/{resourceType}/{resourceId:guid}/share-links")]
    [ProducesResponseType(typeof(CreateShareLinkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateShareLinkResponse>> CreateShareLink(
        string resourceType,
        Guid resourceId,
        CreateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.CreateShareLinkAsync(resourceType, resourceId, request, cancellationToken));
    }

    [HttpDelete("share-links/{shareLinkId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeShareLink(
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        await _shareLinkService.RevokeShareLinkAsync(shareLinkId, cancellationToken);
        return NoContent();
    }

    [HttpGet("resources/{resourceType}/{resourceId:guid}/email-invites")]
    [ProducesResponseType(typeof(EmailInvitesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<EmailInvitesResponse>> GetEmailInvites(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _emailInviteService.GetInvitesAsync(resourceType, resourceId, cancellationToken));
    }

    [HttpPost("resources/{resourceType}/{resourceId:guid}/email-invites")]
    [ProducesResponseType(typeof(CreateEmailInviteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateEmailInviteResponse>> CreateEmailInvite(
        string resourceType,
        Guid resourceId,
        CreateEmailInviteRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _emailInviteService.CreateInviteAsync(resourceType, resourceId, request, cancellationToken));
    }

    [HttpDelete("email-invites/{inviteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RevokeEmailInvite(
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        await _emailInviteService.RevokeInviteAsync(inviteId, cancellationToken);
        return NoContent();
    }

    [HttpPost("email-invites/{inviteId:guid}/retry")]
    [ProducesResponseType(typeof(CreateEmailInviteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CreateEmailInviteResponse>> RetryEmailInvite(
        Guid inviteId,
        CancellationToken cancellationToken)
    {
        return Ok(await _emailInviteService.RetryInviteAsync(inviteId, cancellationToken));
    }

    [HttpGet("email-invites/{token}/resolve")]
    [ProducesResponseType(typeof(ResolveEmailInviteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ResolveEmailInviteResponse>> ResolveEmailInvite(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _emailInviteService.ResolveInviteAsync(token, cancellationToken));
    }

    [HttpPost("email-invites/{token}/accept")]
    [ProducesResponseType(typeof(AcceptEmailInviteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AcceptEmailInviteResponse>> AcceptEmailInvite(
        string token,
        CancellationToken cancellationToken)
    {
        return Ok(await _emailInviteService.AcceptInviteAsync(token, cancellationToken));
    }

    [HttpGet("audit")]
    [ProducesResponseType(typeof(PermissionAuditResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionAuditResponse>> GetAudit(
        [FromQuery] Guid workspaceId,
        [FromQuery] string? resourceType,
        [FromQuery] Guid? resourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _permissionAuditService.GetAuditAsync(
            workspaceId,
            resourceType,
            resourceId,
            cancellationToken));
    }

    [HttpPost("access-requests")]
    [ProducesResponseType(typeof(AccessRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessRequestDto>> CreateAccessRequest(
        CreateAccessRequestRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _accessRequestService.CreateAccessRequestAsync(request, cancellationToken));
    }

    [HttpGet("access-requests")]
    [ProducesResponseType(typeof(AccessRequestsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessRequestsResponse>> GetAccessRequests(
        [FromQuery] Guid workspaceId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        return Ok(await _accessRequestService.GetAccessRequestsAsync(workspaceId, status, cancellationToken));
    }

    [HttpGet("resources/{resourceType}/{resourceId:guid}/access-requests")]
    [ProducesResponseType(typeof(AccessRequestsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessRequestsResponse>> GetResourceAccessRequests(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _accessRequestService.GetResourceAccessRequestsAsync(resourceType, resourceId, cancellationToken));
    }

    [HttpPost("access-requests/{requestId:guid}/review")]
    [ProducesResponseType(typeof(AccessRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessRequestDto>> ReviewAccessRequest(
        Guid requestId,
        ReviewAccessRequestRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _accessRequestService.ReviewAccessRequestAsync(requestId, request, cancellationToken));
    }

    [HttpPost("access-requests/{requestId:guid}/cancel")]
    [ProducesResponseType(typeof(AccessRequestDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<AccessRequestDto>> CancelAccessRequest(
        Guid requestId,
        CancelAccessRequestRequest? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _accessRequestService.CancelAccessRequestAsync(requestId, request, cancellationToken));
    }
}
