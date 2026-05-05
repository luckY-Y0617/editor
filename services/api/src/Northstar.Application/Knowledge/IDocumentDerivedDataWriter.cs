using Northstar.Domain.Knowledge.Documents;

namespace Northstar.Application.Knowledge;

public interface IDocumentDerivedDataWriter
{
    Task RecordDocumentCreatedAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    Task RecordDocumentUpdatedAsync(
        Document document,
        DocumentDraft draft,
        IReadOnlyCollection<string> changedFields,
        bool rebuildLinks,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    Task RecordDocumentMovedAsync(
        Document document,
        DocumentDraft draft,
        Guid? oldCollectionId,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    Task RecordDocumentArchivedAsync(
        Document document,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    Task RecordDocumentRestoredAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    Task RecordDocumentDeletedAsync(
        Document document,
        Guid? actorId,
        CancellationToken cancellationToken = default);

    Task RecordDocumentImportedAsync(
        Document document,
        DocumentDraft draft,
        Guid? actorId,
        CancellationToken cancellationToken = default);
}
