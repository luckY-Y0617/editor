namespace Northstar.Application.Security;

public interface IPermissionResourceDisplayResolver
{
    Task<IReadOnlyList<PermissionResourceDisplaySummary>> GetDisplaySummariesAsync(
        Guid workspaceId,
        IReadOnlyCollection<PermissionResourceReference> resources,
        CancellationToken cancellationToken = default);
}

public sealed record PermissionResourceReference(string ResourceType, Guid ResourceId);

public sealed record PermissionResourceDisplaySummary(
    string ResourceType,
    Guid ResourceId,
    string Title,
    string? Path);
