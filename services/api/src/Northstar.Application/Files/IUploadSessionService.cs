using Northstar.Contracts.Files;

namespace Northstar.Application.Files;

public interface IUploadSessionService
{
    Task<CreateUploadSessionResponse> CreateAsync(
        CreateUploadSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<UploadSessionDto> GetAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<UploadSessionDto> UploadContentAsync(
        Guid sessionId,
        Stream content,
        CancellationToken cancellationToken = default);

    Task<UploadSessionDto> CompleteAsync(
        Guid sessionId,
        CompleteUploadSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<FinalizeUploadSessionResponse> FinalizeAsync(
        Guid sessionId,
        FinalizeUploadSessionRequest request,
        CancellationToken cancellationToken = default);

    Task<UploadSessionDto> GetProgressAsync(Guid sessionId, CancellationToken cancellationToken = default);

    Task<UploadSessionDto> AbortAsync(Guid sessionId, CancellationToken cancellationToken = default);
}
