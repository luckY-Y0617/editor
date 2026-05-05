namespace Northstar.Application.Security;

public interface IEffectivePermissionService
{
    Task<EffectivePermissionResult> AuthorizeWorkspaceAsync(
        Guid workspaceId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default);

    Task<EffectivePermissionResult> AuthorizeCollectionAsync(
        Guid collectionId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<EffectivePermissionResult> AuthorizeDocumentAsync(
        Guid documentId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<IReadOnlyDictionary<Guid, EffectivePermissionResult>> AuthorizeDocumentsAsync(
        IReadOnlyCollection<Guid> documentIds,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default);

    Task<EffectivePermissionResult> AuthorizeDocumentIncludingDeletedAsync(
        Guid documentId,
        Guid userId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null);
}

public sealed record EffectivePermissionResult(
    bool Allowed,
    string? EffectiveRole,
    string Source,
    string? Reason,
    string? InheritanceMode = null);
