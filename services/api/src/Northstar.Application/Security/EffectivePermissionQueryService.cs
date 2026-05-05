using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class EffectivePermissionQueryService : IEffectivePermissionQueryService
{
    private static readonly IReadOnlyList<string> WorkspaceActions =
    [
        PermissionActions.WorkspaceView,
        PermissionActions.WorkspaceManageSettings,
        PermissionActions.WorkspaceManageMembers,
        PermissionActions.WorkspaceManagePermissions,
        PermissionActions.WorkspaceViewAudit
    ];

    private static readonly IReadOnlyList<string> CollectionActions =
    [
        PermissionActions.CollectionView,
        PermissionActions.CollectionEdit,
        PermissionActions.CollectionCreateDocument,
        PermissionActions.CollectionShare,
        PermissionActions.CollectionManagePermissions,
        PermissionActions.CollectionArchive,
        PermissionActions.CollectionDelete
    ];

    private static readonly IReadOnlyList<string> DocumentActions =
    [
        PermissionActions.DocumentView,
        PermissionActions.DocumentComment,
        PermissionActions.DocumentEdit,
        PermissionActions.DocumentShare,
        PermissionActions.DocumentManagePermissions,
        PermissionActions.DocumentArchive,
        PermissionActions.DocumentRestore,
        PermissionActions.DocumentDelete,
        PermissionActions.VersionView,
        PermissionActions.VersionCreate,
        PermissionActions.VersionRestore,
        PermissionActions.FileUpload,
        PermissionActions.FileDownload,
        PermissionActions.FileDelete,
        PermissionActions.AttachmentView,
        PermissionActions.AttachmentCreate,
        PermissionActions.AttachmentDelete,
        PermissionActions.SearchQuery,
        PermissionActions.ActivityView
    ];

    private readonly ICurrentUser _currentUser;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IPermissionCatalog _permissionCatalog;

    public EffectivePermissionQueryService(
        ICurrentUser currentUser,
        IEffectivePermissionService effectivePermissionService,
        IPermissionCatalog permissionCatalog)
    {
        _currentUser = currentUser;
        _effectivePermissionService = effectivePermissionService;
        _permissionCatalog = permissionCatalog;
    }

    public async Task<EffectivePermissionResponse> GetEffectivePermissionAsync(
        string? resourceType,
        Guid resourceId,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var normalizedResourceType = NormalizeResourceType(resourceType);
        var viewResult = await AuthorizeViewAsync(
            normalizedResourceType,
            resourceId,
            _currentUser.UserId.Value,
            cancellationToken);

        if (viewResult.Reason == EffectivePermissionService.ResourceNotFoundReason)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "Resource was not found.");
        }

        if (viewResult.Reason == EffectivePermissionService.NoMembershipReason)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

        if (!viewResult.Allowed)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }

        var actionKeys = ActionKeysFor(normalizedResourceType);
        var allowedActions = _permissionCatalog.GetAllowedActions(viewResult.EffectiveRole, actionKeys);
        return new EffectivePermissionResponse(
            normalizedResourceType,
            resourceId.ToString(),
            allowedActions,
            viewResult.EffectiveRole,
            viewResult.Source,
            viewResult.InheritanceMode ?? InheritanceModes.Inherit);
    }

    private Task<EffectivePermissionResult> AuthorizeViewAsync(
        string resourceType,
        Guid resourceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return resourceType switch
        {
            ResourceTypes.Workspace => _effectivePermissionService.AuthorizeWorkspaceAsync(
                resourceId,
                userId,
                PermissionActions.WorkspaceView,
                cancellationToken),
            ResourceTypes.Collection => _effectivePermissionService.AuthorizeCollectionAsync(
                resourceId,
                userId,
                PermissionActions.CollectionView,
                cancellationToken),
            ResourceTypes.Document => _effectivePermissionService.AuthorizeDocumentAsync(
                resourceId,
                userId,
                PermissionActions.DocumentView,
                cancellationToken),
            _ => throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is invalid.")
        };
    }

    private static IReadOnlyList<string> ActionKeysFor(string resourceType)
    {
        return resourceType switch
        {
            ResourceTypes.Workspace => WorkspaceActions,
            ResourceTypes.Collection => CollectionActions,
            ResourceTypes.Document => DocumentActions,
            _ => throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is invalid.")
        };
    }

    private static string NormalizeResourceType(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is required.");
        }

        var normalized = resourceType.Trim().ToLowerInvariant();
        return ResourceTypes.IsSupported(normalized)
            ? normalized
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, "Resource type is invalid.");
    }
}
