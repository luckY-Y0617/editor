using Northstar.Contracts.Files;

namespace Northstar.Application.Files;

public interface IFileService
{
    Task<FileDto> GetAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task<FileContentResult> OpenContentAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid fileId, CancellationToken cancellationToken = default);
}
