using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface ISpaceTransferRepository
{
    Task<ExportSpaceResponse?> ExportAsync(
        Guid spaceId,
        bool includeArchived,
        CancellationToken cancellationToken = default);

    Task<ImportSpaceResult> ImportAppendAsync(
        Guid spaceId,
        ImportSpaceRequest request,
        Guid actorId,
        CancellationToken cancellationToken = default);
}

public sealed record ImportSpaceResult(
    int ImportedCollectionCount,
    int ImportedDocumentCount);
