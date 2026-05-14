using System.Net;
using System.Security.Cryptography;
using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Contracts.Common;
using Northstar.Contracts.Files;
using Northstar.Domain.Files;

namespace Northstar.Infrastructure.Files;

public sealed class S3ObjectStorage : IObjectStorage
{
    private const int BufferSize = 81920;

    private readonly FilesOptions _options;
    private readonly IAmazonS3 _client;

    public S3ObjectStorage(FilesOptions options)
        : this(options, CreateClient(options))
    {
    }

    internal S3ObjectStorage(FilesOptions options, IAmazonS3 client)
    {
        _options = options;
        _client = client;
    }

    public UploadTargetDto CreateUploadTarget(UploadSession session)
    {
        var expires = DateTime.UtcNow.AddMinutes(Math.Clamp(_options.S3.PresignedUploadMinutes, 1, 60));
        var request = new GetPreSignedUrlRequest
        {
            BucketName = GetBucket(session),
            Key = session.ObjectKey,
            Verb = HttpVerb.PUT,
            Expires = expires,
            ContentType = session.MimeType
        };

        return new UploadTargetDto(
            "s3-compatible",
            "PUT",
            _client.GetPreSignedURL(request),
            new Dictionary<string, string>
            {
                ["Content-Type"] = session.MimeType
            });
    }

    public async Task WriteUploadContentAsync(
        UploadSession session,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken = default)
    {
        var limited = new MaxBytesReadStream(content, Math.Min(maxBytes, session.ByteSize));
        var request = new PutObjectRequest
        {
            BucketName = GetBucket(session),
            Key = session.ObjectKey,
            InputStream = limited,
            ContentType = session.MimeType,
            AutoCloseStream = false
        };
        request.Headers.ContentLength = session.ByteSize;

        await _client.PutObjectAsync(request, cancellationToken);
    }

    public async Task<StoredObjectInfo?> GetObjectInfoAsync(
        UploadSession session,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _client.GetObjectAsync(
                GetBucket(session),
                session.ObjectKey,
                cancellationToken);
            using var sha256 = SHA256.Create();
            var hash = await sha256.ComputeHashAsync(response.ResponseStream, cancellationToken);
            return new StoredObjectInfo(
                response.ContentLength,
                Convert.ToHexString(hash).ToLowerInvariant());
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
            return null;
        }
    }

    public async Task<Stream> OpenReadAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(
                GetBucket(file.Bucket),
                file.ObjectKey,
                cancellationToken);
            return new S3ObjectReadStream(response);
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "File content was not found.");
        }
    }

    public async Task DeleteObjectAsync(StoredFile file, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.DeleteObjectAsync(
                GetBucket(file.Bucket),
                file.ObjectKey,
                cancellationToken);
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
        }
    }

    private static IAmazonS3 CreateClient(FilesOptions options)
    {
        var config = new AmazonS3Config
        {
            ForcePathStyle = options.S3.ForcePathStyle,
            UseHttp = options.S3.UseHttp
        };

        if (!string.IsNullOrWhiteSpace(options.S3.Endpoint))
        {
            config.ServiceURL = options.S3.Endpoint.Trim();
        }
        else
        {
            config.RegionEndpoint = RegionEndpoint.GetBySystemName(
                string.IsNullOrWhiteSpace(options.S3.Region)
                    ? "us-east-1"
                    : options.S3.Region.Trim());
        }

        if (!string.IsNullOrWhiteSpace(options.S3.AccessKey) &&
            !string.IsNullOrWhiteSpace(options.S3.SecretKey))
        {
            return new AmazonS3Client(
                new BasicAWSCredentials(options.S3.AccessKey.Trim(), options.S3.SecretKey),
                config);
        }

        return new AmazonS3Client(config);
    }

    private static bool IsNotFound(AmazonS3Exception exception)
    {
        return exception.StatusCode == HttpStatusCode.NotFound ||
            string.Equals(exception.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(exception.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase);
    }

    private string GetBucket(UploadSession session)
    {
        return GetBucket(session.Bucket);
    }

    private string GetBucket(string? bucket)
    {
        var selected = string.IsNullOrWhiteSpace(bucket) ? _options.DefaultBucket : bucket.Trim();
        if (string.IsNullOrWhiteSpace(selected))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "File storage bucket is not configured.");
        }

        return selected;
    }

    private sealed class S3ObjectReadStream : Stream
    {
        private readonly GetObjectResponse _response;
        private readonly Stream _inner;
        private readonly long _length;

        public S3ObjectReadStream(GetObjectResponse response)
        {
            _response = response;
            _inner = response.ResponseStream;
            _length = response.ContentLength;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _length;
        public override long Position
        {
            get => _inner.CanSeek ? _inner.Position : 0;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            return _inner.ReadAsync(buffer, cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _response.Dispose();
            }

            base.Dispose(disposing);
        }
    }

    private sealed class MaxBytesReadStream : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private long _readBytes;

        public MaxBytesReadStream(Stream inner, long maxBytes)
        {
            _inner = inner;
            _maxBytes = maxBytes;
        }

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _maxBytes;
        public override long Position
        {
            get => _readBytes;
            set => throw new NotSupportedException();
        }

        public override void Flush()
        {
            _inner.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _inner.Read(buffer, offset, count);
            Track(read);
            return read;
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            var read = await _inner.ReadAsync(buffer, cancellationToken);
            Track(read);
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        private void Track(int bytesRead)
        {
            _readBytes += bytesRead;
            if (_readBytes > _maxBytes)
            {
                throw new ApplicationErrorException(
                    ErrorCodes.ValidationError,
                    "Uploaded content exceeds the configured size limit.");
            }
        }
    }
}
