namespace Northstar.Application.Security;

public interface IScopedResourceAccessService
{
    Task<Guid> GetRequiredUserIdAsync(CancellationToken cancellationToken = default);

    Task<EffectivePermissionResult> EnsureCanAccessDocumentAsync(
        Guid documentId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<EffectivePermissionResult> EnsureCanAccessDocumentIncludingDeletedAsync(
        Guid documentId,
        string actionKey,
        CancellationToken cancellationToken = default,
        string? shareToken = null);

    Task<EffectivePermissionResult> EnsureCanAccessCollectionAsync(
        Guid collectionId,
        string actionKey,
        CancellationToken cancellationToken = default);

    Task<EffectivePermissionResult> EnsureCanAccessDocumentAnyAsync(
        Guid documentId,
        IReadOnlyList<string> actionKeys,
        CancellationToken cancellationToken = default,
        string? shareToken = null);
}
