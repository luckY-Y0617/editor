namespace Northstar.Application.Security;

public interface IResourceWorkspaceResolver
{
    Task<Guid?> GetWorkspaceIdForSpaceAsync(Guid spaceId, CancellationToken cancellationToken = default);
    Task<LibraryPermissionResource?> GetLibraryPermissionResourceAsync(Guid libraryId, CancellationToken cancellationToken = default);
    Task<Guid?> GetWorkspaceIdForDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<Guid?> GetWorkspaceIdForCollectionAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<DocumentPermissionResource?> GetDocumentPermissionResourceAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<DocumentPermissionResource?> GetDocumentPermissionResourceIncludingDeletedAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentPermissionResource>> GetDocumentPermissionResourcesAsync(IReadOnlyCollection<Guid> documentIds, CancellationToken cancellationToken = default);
    Task<CollectionPermissionResource?> GetCollectionPermissionResourceAsync(Guid collectionId, CancellationToken cancellationToken = default);
}

public sealed record LibraryPermissionResource(Guid LibraryId, Guid WorkspaceId);
public sealed record DocumentPermissionResource(Guid DocumentId, Guid WorkspaceId, Guid? CollectionId);

public sealed record CollectionPermissionResource(Guid CollectionId, Guid WorkspaceId);
