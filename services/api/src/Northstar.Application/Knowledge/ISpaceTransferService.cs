using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Knowledge;

public interface ISpaceTransferService
{
    Task<ExportSpaceResponse> ExportAsync(
        Guid spaceId,
        bool includeArchived = true,
        CancellationToken cancellationToken = default);

    Task<ImportSpaceResponse> ImportAsync(
        Guid spaceId,
        ImportSpaceRequest request,
        CancellationToken cancellationToken = default);
}
