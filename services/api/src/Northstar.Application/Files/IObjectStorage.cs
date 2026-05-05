using Northstar.Contracts.Files;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public interface IObjectStorage
{
    UploadTargetDto CreateUploadTarget(UploadSession session);

    Task WriteUploadContentAsync(
        UploadSession session,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken = default);

    Task<StoredObjectInfo?> GetObjectInfoAsync(
        UploadSession session,
        CancellationToken cancellationToken = default);

    Task<Stream> OpenReadAsync(StoredFile file, CancellationToken cancellationToken = default);

    Task DeleteObjectAsync(StoredFile file, CancellationToken cancellationToken = default);
}
