using Northstar.Domain.Knowledge.Documents;

namespace Northstar.Application.Knowledge;

public interface IDocumentRepository
{
    Task<CollectionLocation?> GetCollectionLocationAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task<decimal> GetNextDocumentSortOrderAsync(Guid collectionId, CancellationToken cancellationToken = default);
    Task AddDocumentAsync(Document document, DocumentDraft draft, CancellationToken cancellationToken = default);
    Task<DocumentEditState?> GetDocumentEditStateAsync(Guid documentId, bool includeDeleted = false, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetDocumentTagNamesAsync(Guid documentId, CancellationToken cancellationToken = default);
    Task ReplaceDocumentTagsAsync(
        Guid workspaceId,
        Guid documentId,
        IReadOnlyCollection<string> tagNames,
        Guid? actorId,
        CancellationToken cancellationToken = default);
}
