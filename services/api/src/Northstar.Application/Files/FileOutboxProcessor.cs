using Northstar.Application.Common;
using Northstar.Domain.Files;

namespace Northstar.Application.Files;

public sealed class FileOutboxProcessor : IFileOutboxProcessor
{
    private readonly IFileRepository _fileRepository;
    private readonly IObjectStorage _objectStorage;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly FileOutboxOptions _options;

    public FileOutboxProcessor(
        IFileRepository fileRepository,
        IObjectStorage objectStorage,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork,
        FileOutboxOptions options)
    {
        _fileRepository = fileRepository;
        _objectStorage = objectStorage;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
        _options = options;
    }

    public Task<FileOutboxProcessResult> ProcessDueAsync(
        DateTimeOffset? now = null,
        int batchSize = 25,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var timestamp = now ?? DateTimeOffset.UtcNow;
            var dueEvents = await _fileRepository.GetDueOutboxEventsForUpdateAsync(
                timestamp,
                Math.Clamp(batchSize, 1, 100),
                ct);
            var published = 0;
            var retrying = 0;
            var failed = 0;
            var skipped = 0;

            foreach (var outboxEvent in dueEvents)
            {
                var status = await ProcessEventAsync(outboxEvent, timestamp, ct);
                switch (status)
                {
                    case FileOutboxProcessingStatus.Published:
                        published++;
                        break;
                    case FileOutboxProcessingStatus.Retrying:
                        retrying++;
                        break;
                    case FileOutboxProcessingStatus.Failed:
                        failed++;
                        break;
                    default:
                        skipped++;
                        break;
                }
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return new FileOutboxProcessResult(dueEvents.Count, published, retrying, failed, skipped);
        }, cancellationToken);
    }

    private async Task<FileOutboxProcessingStatus> ProcessEventAsync(
        FileOutboxEvent outboxEvent,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        try
        {
            switch (outboxEvent.EventType)
            {
                case FileOutboxEventTypes.FileFinalized:
                case FileOutboxEventTypes.DocumentAttachmentCreated:
                    outboxEvent.MarkPublished(now);
                    return FileOutboxProcessingStatus.Published;

                case FileOutboxEventTypes.FileDeleted:
                    var file = await _fileRepository.GetFileAsync(
                        outboxEvent.AggregateId,
                        includeDeleted: true,
                        cancellationToken);
                    if (file is null)
                    {
                        outboxEvent.MarkPublished(now);
                        return FileOutboxProcessingStatus.Published;
                    }

                    await _objectStorage.DeleteObjectAsync(file, cancellationToken);
                    outboxEvent.MarkPublished(now);
                    return FileOutboxProcessingStatus.Published;

                default:
                    outboxEvent.MarkPublished(now);
                    return FileOutboxProcessingStatus.Skipped;
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            var nextRetryAt = now.AddSeconds(Math.Max(1, _options.RetryDelaySeconds));
            outboxEvent.MarkFailure(
                now,
                exception.Message,
                nextRetryAt,
                Math.Max(1, _options.MaxAttempts));
            return outboxEvent.Status == FileOutboxEventStatus.Failed
                ? FileOutboxProcessingStatus.Failed
                : FileOutboxProcessingStatus.Retrying;
        }
    }

    private enum FileOutboxProcessingStatus
    {
        Published,
        Retrying,
        Failed,
        Skipped
    }
}
