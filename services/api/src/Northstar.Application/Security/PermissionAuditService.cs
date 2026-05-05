using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionAuditService : IPermissionAuditService
{
    private readonly IPermissionAuditRepository _repository;
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IScopedResourceAccessService _scopedResourceAccessService;

    public PermissionAuditService(
        IPermissionAuditRepository repository,
        IWorkspaceAccessService workspaceAccessService,
        IScopedResourceAccessService scopedResourceAccessService)
    {
        _repository = repository;
        _workspaceAccessService = workspaceAccessService;
        _scopedResourceAccessService = scopedResourceAccessService;
    }

    public Task AddAsync(PermissionAuditEvent auditEvent, CancellationToken cancellationToken = default)
    {
        return _repository.AddAsync(auditEvent, cancellationToken);
    }

    public async Task<PermissionAuditResponse> GetAuditAsync(
        Guid workspaceId,
        string? resourceType,
        Guid? resourceId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resourceType))
        {
            await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        }
        else
        {
            if (resourceId is null)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceId is required when resourceType is provided.");
            }

            var normalizedResourceType = PermissionResourceNormalizer.NormalizeScopedResourceType(resourceType);
            if (normalizedResourceType == ResourceTypes.Document)
            {
                await _scopedResourceAccessService.EnsureCanAccessDocumentAsync(
                    resourceId.Value,
                    PermissionActions.DocumentManagePermissions,
                    cancellationToken);
            }
            else
            {
                await _scopedResourceAccessService.EnsureCanAccessCollectionAsync(
                    resourceId.Value,
                    PermissionActions.CollectionManagePermissions,
                    cancellationToken);
            }

            resourceType = normalizedResourceType;
        }

        var events = await _repository.GetAsync(workspaceId, resourceType, resourceId, cancellationToken);
        return new PermissionAuditResponse(events.Select(ToDto).ToArray());
    }

    private static PermissionAuditEventDto ToDto(PermissionAuditEvent auditEvent)
    {
        return new PermissionAuditEventDto(
            auditEvent.Id.ToString(),
            auditEvent.WorkspaceId.ToString(),
            auditEvent.ActorId?.ToString(),
            auditEvent.Action,
            auditEvent.ResourceType,
            auditEvent.ResourceId.ToString(),
            auditEvent.SubjectType,
            auditEvent.SubjectId?.ToString(),
            auditEvent.BeforeJson,
            auditEvent.AfterJson,
            auditEvent.Metadata,
            auditEvent.CreatedAt);
    }
}
