namespace Northstar.Application.Files;

public interface IFileOutboxProcessor
{
    Task<FileOutboxProcessResult> ProcessDueAsync(
        DateTimeOffset? now = null,
        int batchSize = 25,
        CancellationToken cancellationToken = default);
}

public sealed record FileOutboxProcessResult(
    int Attempted,
    int Published,
    int Retrying,
    int Failed,
    int Skipped);
