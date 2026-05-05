using System.Security.Cryptography;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;
using Northstar.Domain.Files;

namespace Northstar.Infrastructure.Files;

public sealed class LocalFileStorage : IObjectStorage
{
    private const int BufferSize = 81920;

    private readonly FilesOptions _options;

    public LocalFileStorage(FilesOptions options)
    {
        _options = options;
    }

    public UploadTargetDto CreateUploadTarget(UploadSession session)
    {
        return new UploadTargetDto(
            "local-api",
            "PUT",
            $"/api/v1/files/uploads/sessions/{session.Id}/content",
            new Dictionary<string, string>());
    }

    public async Task WriteUploadContentAsync(
        UploadSession session,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(session.ObjectKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var output = new FileStream(
            path,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            useAsync: true);

        var buffer = new byte[BufferSize];
        long totalBytes = 0;
        int bytesRead;
        while ((bytesRead = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
        {
            totalBytes += bytesRead;
            if (totalBytes > maxBytes || totalBytes > session.ByteSize)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Uploaded content exceeds the configured size limit.");
            }

            await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }
    }

    public async Task<StoredObjectInfo?> GetObjectInfoAsync(
        UploadSession session,
        CancellationToken cancellationToken = default)
    {
        var path = GetPath(session.ObjectKey);
        if (!System.IO.File.Exists(path))
        {
            return null;
        }

        await using var input = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            useAsync: true);
        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(input, cancellationToken);
        return new StoredObjectInfo(input.Length, Convert.ToHexString(hash).ToLowerInvariant());
    }

    public Task<Stream> OpenReadAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        var path = GetPath(file.ObjectKey);
        if (!System.IO.File.Exists(path))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "File content was not found.");
        }

        Stream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            BufferSize,
            useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteObjectAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        var path = GetPath(file.ObjectKey);
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return Task.CompletedTask;
    }

    private string GetPath(string objectKey)
    {
        var root = Path.GetFullPath(_options.LocalRootPath);
        var relativeParts = objectKey.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var path = Path.GetFullPath(Path.Combine([root, .. relativeParts]));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Invalid storage object key.");
        }

        return path;
    }
}
