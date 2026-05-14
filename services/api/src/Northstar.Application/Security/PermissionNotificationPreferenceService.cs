using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionNotificationPreferenceService : IPermissionNotificationPreferenceService
{
    private readonly IPermissionNotificationPreferenceRepository _repository;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IScopedResourceAccessService _scopedResourceAccessService;
    private readonly IResourceWorkspaceResolver _resourceWorkspaceResolver;
    private readonly IPermissionResourceDisplayResolver _resourceDisplayResolver;
    private readonly IUnitOfWork _unitOfWork;

    public PermissionNotificationPreferenceService(
        IPermissionNotificationPreferenceRepository repository,
        IWorkspaceAccessService workspaceAccessService,
        IScopedResourceAccessService scopedResourceAccessService,
        IResourceWorkspaceResolver resourceWorkspaceResolver,
        IPermissionResourceDisplayResolver resourceDisplayResolver,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _workspaceAccessService = workspaceAccessService;
        _scopedResourceAccessService = scopedResourceAccessService;
        _resourceWorkspaceResolver = resourceWorkspaceResolver;
        _resourceDisplayResolver = resourceDisplayResolver;
        _unitOfWork = unitOfWork;
    }

    public async Task<PermissionNotificationPreferencesResponse> GetPreferencesAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        var userId = await _workspaceAccessService.GetRequiredUserIdAsync(cancellationToken);
        await _workspaceAccessService.EnsureCanViewWorkspaceAsync(workspaceId, cancellationToken);
        var preferences = await _repository.GetForUserWorkspaceAsync(userId, workspaceId, cancellationToken);
        var summaries = await ResolveDisplaySummariesAsync(workspaceId, preferences, cancellationToken);

        return new PermissionNotificationPreferencesResponse(
            preferences
                .Select(preference => ToDto(preference, GetDisplaySummary(summaries, preference)))
                .ToArray());
    }

    public async Task<PermissionNotificationPreferenceDto> UpdatePreferenceAsync(
        UpdatePermissionNotificationPreferenceRequest request,
        CancellationToken cancellationToken = default)
    {
        var workspaceId = ParseGuid(request.WorkspaceId, "workspaceId");
        var scope = NormalizeScope(request.ResourceType, request.ResourceId);
        var userId = await _workspaceAccessService.GetRequiredUserIdAsync(cancellationToken);

        if (scope.ResourceType is null)
        {
            await _workspaceAccessService.EnsureCanViewWorkspaceAsync(workspaceId, cancellationToken);
        }
        else
        {
            await EnsureCanSetResourcePreferenceAsync(workspaceId, scope, cancellationToken);
        }

        var preference = await _repository.GetForUpdateAsync(
            userId,
            workspaceId,
            scope.ResourceType,
            scope.ResourceId,
            cancellationToken);
        if (preference is null)
        {
            preference = new PermissionNotificationPreference(
                workspaceId,
                userId,
                scope.ResourceType,
                scope.ResourceId,
                request.Watched,
                request.Muted);
            await _repository.AddAsync(preference, cancellationToken);
        }
        else
        {
            preference.SetState(request.Watched, request.Muted);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var summaries = await ResolveDisplaySummariesAsync(workspaceId, new[] { preference }, cancellationToken);
        return ToDto(preference, GetDisplaySummary(summaries, preference));
    }

    private async Task EnsureCanSetResourcePreferenceAsync(
        Guid workspaceId,
        PreferenceScope scope,
        CancellationToken cancellationToken)
    {
        if (scope.ResourceType == ResourceTypes.Document)
        {
            var document = await _resourceWorkspaceResolver.GetDocumentPermissionResourceAsync(
                scope.ResourceId!.Value,
                cancellationToken)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            if (document.WorkspaceId != workspaceId)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceId must belong to workspaceId.");
            }

            await _scopedResourceAccessService.EnsureCanAccessDocumentAsync(
                scope.ResourceId.Value,
                PermissionActions.DocumentView,
                cancellationToken);
            return;
        }

        var collection = await _resourceWorkspaceResolver.GetCollectionPermissionResourceAsync(
            scope.ResourceId!.Value,
            cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Collection was not found.");
        if (collection.WorkspaceId != workspaceId)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceId must belong to workspaceId.");
        }

        await _scopedResourceAccessService.EnsureCanAccessCollectionAsync(
            scope.ResourceId.Value,
            PermissionActions.CollectionView,
            cancellationToken);
    }

    private static PreferenceScope NormalizeScope(string? resourceType, string? resourceId)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            if (!string.IsNullOrWhiteSpace(resourceId))
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceType is required when resourceId is provided.");
            }

            return new PreferenceScope(null, null);
        }

        if (string.IsNullOrWhiteSpace(resourceId))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceId is required when resourceType is provided.");
        }

        var normalizedResourceType = resourceType.Trim().ToLowerInvariant();
        if (!ResourceTypes.IsScopedResource(normalizedResourceType))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceType must be document or collection.");
        }

        return new PreferenceScope(
            normalizedResourceType,
            ParseGuid(resourceId, "resourceId"));
    }

    private static Guid ParseGuid(string value, string fieldName)
    {
        return Guid.TryParse(value, out var parsed)
            ? parsed
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a valid UUID.");
    }

    private async Task<IReadOnlyDictionary<PreferenceScope, PermissionResourceDisplaySummary>> ResolveDisplaySummariesAsync(
        Guid workspaceId,
        IReadOnlyCollection<PermissionNotificationPreference> preferences,
        CancellationToken cancellationToken)
    {
        var references = new List<PermissionResourceReference>();
        foreach (var preference in preferences)
        {
            if (preference.ResourceType is null || preference.ResourceId is null)
            {
                continue;
            }

            if (await CanViewResourceAsync(preference.ResourceType, preference.ResourceId.Value, cancellationToken))
            {
                references.Add(new PermissionResourceReference(preference.ResourceType, preference.ResourceId.Value));
            }
        }

        if (references.Count == 0)
        {
            return new Dictionary<PreferenceScope, PermissionResourceDisplaySummary>();
        }

        var summaries = await _resourceDisplayResolver.GetDisplaySummariesAsync(workspaceId, references, cancellationToken);
        return summaries.ToDictionary(
            summary => new PreferenceScope(summary.ResourceType, summary.ResourceId),
            summary => summary);
    }

    private async Task<bool> CanViewResourceAsync(
        string resourceType,
        Guid resourceId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (resourceType == ResourceTypes.Document)
            {
                await _scopedResourceAccessService.EnsureCanAccessDocumentAsync(
                    resourceId,
                    PermissionActions.DocumentView,
                    cancellationToken);
                return true;
            }

            await _scopedResourceAccessService.EnsureCanAccessCollectionAsync(
                resourceId,
                PermissionActions.CollectionView,
                cancellationToken);
            return true;
        }
        catch (ApplicationErrorException exception) when (
            exception.Code is ErrorCodes.Forbidden or ErrorCodes.NotFound)
        {
            return false;
        }
    }

    private static PermissionResourceDisplaySummary? GetDisplaySummary(
        IReadOnlyDictionary<PreferenceScope, PermissionResourceDisplaySummary> summaries,
        PermissionNotificationPreference preference)
    {
        return preference.ResourceType is null || preference.ResourceId is null
            ? null
            : summaries.GetValueOrDefault(new PreferenceScope(preference.ResourceType, preference.ResourceId.Value));
    }

    private static PermissionNotificationPreferenceDto ToDto(
        PermissionNotificationPreference preference,
        PermissionResourceDisplaySummary? resource)
    {
        return new PermissionNotificationPreferenceDto(
            preference.Id.ToString(),
            preference.WorkspaceId.ToString(),
            preference.UserId.ToString(),
            preference.ResourceType,
            preference.ResourceId?.ToString(),
            preference.Watched,
            preference.Muted,
            preference.CreatedAt,
            preference.UpdatedAt,
            resource is null
                ? null
                : new PermissionNotificationPreferenceResourceDto(
                    resource.ResourceType,
                    resource.ResourceId.ToString(),
                    resource.Title,
                    resource.Path));
    }

    private sealed record PreferenceScope(string? ResourceType, Guid? ResourceId);
}
