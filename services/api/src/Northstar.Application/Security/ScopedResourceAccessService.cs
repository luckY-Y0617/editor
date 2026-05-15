using Northstar.Application.Common;
using Northstar.Contracts.Common;

namespace Northstar.Application.Security;

public sealed class ScopedResourceAccessService : IScopedResourceAccessService
{
    private readonly ICurrentUser _currentUser;
    private readonly IEffectivePermissionService _effectivePermissionService;
    private readonly IShareLinkAccessAuditService _shareLinkAccessAuditService;

    public ScopedResourceAccessService(
        ICurrentUser currentUser,
        IEffectivePermissionService effectivePermissionService,
        IShareLinkAccessAuditService shareLinkAccessAuditService)
    {
        _currentUser = currentUser;
        _effectivePermissionService = effectivePermissionService;
        _shareLinkAccessAuditService = shareLinkAccessAuditService;
    }

    public Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        return Task.FromResult(_currentUser.UserId.Value);
    }

    public async Task<EffectivePermissionResult> EnsureCanAccessDocumentAsync(
        Guid documentId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var result = await _effectivePermissionService.AuthorizeDocumentAsync(
            documentId,
            userId,
            actionKey,
            cancellationToken,
            shareToken);

        await _shareLinkAccessAuditService.RecordProtectedResourceAccessAsync(
            shareToken,
            documentId,
            result,
            cancellationToken);
        EnsureAllowed(result, "Document was not found.");
        return result;
    }

    public async Task<EffectivePermissionResult> EnsureCanAccessDocumentIncludingDeletedAsync(
        Guid documentId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var result = await _effectivePermissionService.AuthorizeDocumentIncludingDeletedAsync(
            documentId,
            userId,
            actionKey,
            cancellationToken,
            shareToken);

        await _shareLinkAccessAuditService.RecordProtectedResourceAccessAsync(
            shareToken,
            documentId,
            result,
            cancellationToken);
        EnsureAllowed(result, "Document was not found.");
        return result;
    }

    public async Task<EffectivePermissionResult> EnsureCanAccessCollectionAsync(
        Guid collectionId,
        string actionKey,
        CancellationToken cancellationToken = default)
    {
        var userId = await GetRequiredUserIdAsync(cancellationToken);
        var result = await _effectivePermissionService.AuthorizeCollectionAsync(
            collectionId,
            userId,
            actionKey,
            cancellationToken);

        EnsureAllowed(result, "Collection was not found.");
        return result;
    }

    public async Task<EffectivePermissionResult> EnsureCanAccessDocumentAnyAsync(
        Guid documentId,
        IReadOnlyList<string> actionKeys,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        if (actionKeys.Count == 0)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "At least one action key is required.");
        }

        EffectivePermissionResult? lastResult = null;
        foreach (var actionKey in actionKeys)
        {
            var userId = await GetRequiredUserIdAsync(cancellationToken);
            var result = await _effectivePermissionService.AuthorizeDocumentAsync(
                documentId,
                userId,
                actionKey,
                cancellationToken,
                shareToken);
            await _shareLinkAccessAuditService.RecordProtectedResourceAccessAsync(
                shareToken,
                documentId,
                result,
                cancellationToken);
            if (result.Allowed)
            {
                return result;
            }

            lastResult = result;
        }

        EnsureAllowed(lastResult!, "Document was not found.");
        return lastResult!;
    }

    private static void EnsureAllowed(EffectivePermissionResult result, string notFoundMessage)
    {
        if (result.Reason == EffectivePermissionService.ResourceNotFoundReason)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, notFoundMessage);
        }

        if (result.Reason == EffectivePermissionService.NoMembershipReason)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace access is denied.");
        }

        if (!result.Allowed)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "Workspace permission is insufficient.");
        }
    }
}
