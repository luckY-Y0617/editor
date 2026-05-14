using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class PermissionAuditService : IPermissionAuditService
{
    private const int DefaultWorkspaceAuditLimit = 50;
    private const int MaxWorkspaceAuditLimit = 100;

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

    public async Task<WorkspaceAuditLogResponse> GetWorkspaceAuditLogAsync(
        Guid workspaceId,
        string? action,
        string? resourceType,
        Guid? resourceId,
        Guid? actorId,
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? offset,
        int? limit,
        CancellationToken cancellationToken = default)
    {
        await _workspaceAccessService.EnsureCanAccessWorkspaceAsync(
            workspaceId,
            PermissionActions.WorkspaceViewAudit,
            cancellationToken);

        var normalizedAction = string.IsNullOrWhiteSpace(action) ? null : action.Trim();
        var normalizedResourceType = string.IsNullOrWhiteSpace(resourceType)
            ? null
            : PermissionResourceNormalizer.NormalizeSupportedResourceType(resourceType);
        if (resourceId.HasValue && normalizedResourceType is null)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "resourceType is required when resourceId is provided.");
        }

        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "from must be earlier than or equal to to.");
        }

        var normalizedOffset = Math.Max(0, offset ?? 0);
        var normalizedLimit = Math.Clamp(limit ?? DefaultWorkspaceAuditLimit, 1, MaxWorkspaceAuditLimit);
        var page = await _repository.SearchWorkspaceAsync(
            new PermissionAuditQuery(
                workspaceId,
                normalizedAction,
                normalizedResourceType,
                resourceId,
                actorId,
                from,
                to,
                normalizedOffset,
                normalizedLimit),
            cancellationToken);

        return new WorkspaceAuditLogResponse(
            page.Events.Select(ToWorkspaceAuditDto).ToArray(),
            normalizedOffset,
            normalizedLimit,
            page.TotalCount,
            normalizedOffset + page.Events.Count < page.TotalCount);
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

    private static WorkspaceAuditEventDto ToWorkspaceAuditDto(PermissionAuditRow row)
    {
        var auditEvent = row.Event;
        return new WorkspaceAuditEventDto(
            auditEvent.Id.ToString(),
            auditEvent.WorkspaceId.ToString(),
            auditEvent.ActorId?.ToString(),
            row.ActorName,
            row.ActorEmail,
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
