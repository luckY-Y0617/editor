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
    private readonly IShareLinkAccessAuditService _shareLinkAccessAuditService;
    private readonly IEmailInviteService _emailInviteService;

    public PermissionsController(
        IEffectivePermissionQueryService permissionQueryService,
        IResourcePermissionManagementService permissionManagementService,
        IPermissionAuditService permissionAuditService,
        IAccessRequestService accessRequestService,
        IShareLinkService shareLinkService,
        IShareLinkAccessAuditService shareLinkAccessAuditService,
        IEmailInviteService emailInviteService)
    {
        _permissionQueryService = permissionQueryService;
        _permissionManagementService = permissionManagementService;
        _permissionAuditService = permissionAuditService;
        _accessRequestService = accessRequestService;
        _shareLinkService = shareLinkService;
        _shareLinkAccessAuditService = shareLinkAccessAuditService;
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

    [HttpGet("share-links")]
    [ProducesResponseType(typeof(LinkManagementListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<LinkManagementListResponse>> SearchShareLinks(
        [FromQuery] Guid workspaceId,
        [FromQuery] string? resourceType,
        [FromQuery] Guid? resourceId,
        [FromQuery] string? audience,
        [FromQuery] string? roleKey,
        [FromQuery] string? status,
        [FromQuery] string? q,
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.SearchShareLinksAsync(
            workspaceId,
            resourceType,
            resourceId,
            audience,
            roleKey,
            status,
            q,
            offset,
            limit,
            cancellationToken));
    }

    [HttpGet("share-links/{shareLinkId:guid}")]
    [ProducesResponseType(typeof(LinkManagementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LinkManagementDto>> GetShareLink(
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.GetManagedShareLinkAsync(shareLinkId, cancellationToken));
    }

    [HttpPatch("share-links/{shareLinkId:guid}")]
    [ProducesResponseType(typeof(LinkManagementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LinkManagementDto>> UpdateShareLink(
        Guid shareLinkId,
        UpdateShareLinkRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.UpdateShareLinkAsync(shareLinkId, request, cancellationToken));
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

    [HttpPost("share-links/{shareLinkId:guid}/pause")]
    [ProducesResponseType(typeof(LinkManagementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LinkManagementDto>> PauseShareLink(
        Guid shareLinkId,
        ShareLinkPauseRequest? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.PauseShareLinkAsync(shareLinkId, request, cancellationToken));
    }

    [HttpPost("share-links/{shareLinkId:guid}/resume")]
    [ProducesResponseType(typeof(LinkManagementDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<LinkManagementDto>> ResumeShareLink(
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.ResumeShareLinkAsync(shareLinkId, cancellationToken));
    }

    [HttpPost("share-links/{shareLinkId:guid}/copy")]
    [ProducesResponseType(typeof(CopyShareLinkResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CopyShareLinkResponse>> CopyShareLink(
        Guid shareLinkId,
        ShareLinkCopyEventRequest? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkService.CopyShareLinkAsync(shareLinkId, request, cancellationToken));
    }

    [HttpPost("share-links/{shareLinkId:guid}/copy-events")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> RecordShareLinkCopyEvent(
        Guid shareLinkId,
        ShareLinkCopyEventRequest? request,
        CancellationToken cancellationToken)
    {
        await _shareLinkService.RecordCopyEventAsync(shareLinkId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("share-links/{shareLinkId:guid}/stats")]
    [ProducesResponseType(typeof(ShareLinkAccessStatsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShareLinkAccessStatsResponse>> GetShareLinkStats(
        Guid shareLinkId,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkAccessAuditService.GetStatsAsync(shareLinkId, cancellationToken));
    }

    [HttpGet("share-links/{shareLinkId:guid}/access-events")]
    [ProducesResponseType(typeof(ShareLinkAccessEventsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShareLinkAccessEventsResponse>> GetShareLinkAccessEvents(
        Guid shareLinkId,
        [FromQuery] int? offset,
        [FromQuery] int? limit,
        [FromQuery] string? result,
        [FromQuery] string? eventType,
        CancellationToken cancellationToken)
    {
        return Ok(await _shareLinkAccessAuditService.GetEventsAsync(
            shareLinkId,
            result,
            eventType,
            offset,
            limit,
            cancellationToken));
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
