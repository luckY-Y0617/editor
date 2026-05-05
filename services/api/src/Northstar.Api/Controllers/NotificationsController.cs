using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;

namespace Northstar.Api.Controllers;

[Authorize]
[ApiController]
[Route("notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly IPermissionNotificationService _notificationService;
    private readonly IPermissionNotificationPreferenceService _preferenceService;

    public NotificationsController(
        IPermissionNotificationService notificationService,
        IPermissionNotificationPreferenceService preferenceService)
    {
        _notificationService = notificationService;
        _preferenceService = preferenceService;
    }

    [HttpGet("preferences")]
    [ProducesResponseType(typeof(PermissionNotificationPreferencesResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionNotificationPreferencesResponse>> GetPreferences(
        [FromQuery] Guid workspaceId,
        CancellationToken cancellationToken)
    {
        return Ok(await _preferenceService.GetPreferencesAsync(workspaceId, cancellationToken));
    }

    [HttpPut("preferences")]
    [ProducesResponseType(typeof(PermissionNotificationPreferenceDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionNotificationPreferenceDto>> UpdatePreference(
        UpdatePermissionNotificationPreferenceRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _preferenceService.UpdatePreferenceAsync(request, cancellationToken));
    }

    [HttpGet]
    [ProducesResponseType(typeof(PermissionNotificationsResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionNotificationsResponse>> GetNotifications(
        [FromQuery] Guid? workspaceId,
        [FromQuery] bool unreadOnly,
        CancellationToken cancellationToken)
    {
        return Ok(await _notificationService.GetNotificationsAsync(workspaceId, unreadOnly, cancellationToken));
    }

    [HttpPatch("{notificationId:guid}/read")]
    [ProducesResponseType(typeof(PermissionNotificationDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<PermissionNotificationDto>> MarkRead(
        Guid notificationId,
        MarkNotificationReadRequest request,
        CancellationToken cancellationToken)
    {
        _ = request;
        return Ok(await _notificationService.MarkReadAsync(notificationId, cancellationToken));
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> MarkAllRead(
        MarkAllNotificationsReadRequest? request,
        CancellationToken cancellationToken)
    {
        var workspaceId = ParseOptionalWorkspaceId(request?.WorkspaceId);
        await _notificationService.MarkAllReadAsync(workspaceId, cancellationToken);
        return NoContent();
    }

    private static Guid? ParseOptionalWorkspaceId(string? workspaceId)
    {
        if (string.IsNullOrWhiteSpace(workspaceId))
        {
            return null;
        }

        return Guid.TryParse(workspaceId, out var parsed)
            ? parsed
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "workspaceId must be a valid UUID.");
    }
}
