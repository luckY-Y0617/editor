using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Comments;
using Northstar.Domain.Security;

namespace Northstar.Application.Knowledge;

public sealed class CommentService : ICommentService
{
    private const string AnchorSchema = "northstar.commentAnchor.v1";
    private const string TextRangeKind = "tiptap.textRange";

    private readonly ICommentRepository _commentRepository;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;

    public CommentService(
        ICommentRepository commentRepository,
        IScopedResourceAccessService scopedAccessService,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork)
    {
        _commentRepository = commentRepository;
        _scopedAccessService = scopedAccessService;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
    }

    public async Task<CommentThreadsResponse> ListThreadsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.DocumentView,
            cancellationToken,
            shareToken);

        return new CommentThreadsResponse(
            await _commentRepository.ListThreadDtosAsync(documentId, cancellationToken));
    }

    public Task<CommentThreadDto> CreateThreadAsync(
        Guid documentId,
        CreateCommentThreadRequest request,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentComment,
                ct,
                shareToken);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var body = NormalizeBody(request.Body);
            var anchorJson = NormalizeAnchorJson(documentId, request.Anchor);

            var thread = new CommentThread(documentId, anchorJson);
            var firstMessage = new CommentMessage(thread.Id, body, actorId);

            await _commentRepository.AddThreadAsync(thread, firstMessage, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return await GetThreadDtoOrThrowAsync(documentId, thread.Id, ct);
        }, cancellationToken);
    }

    public Task<CommentThreadDto> AddMessageAsync(
        Guid documentId,
        Guid threadId,
        AddCommentMessageRequest request,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentComment,
                ct,
                shareToken);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var body = NormalizeBody(request.Body);
            var thread = await GetThreadOrThrowAsync(documentId, threadId, ct);

            var message = new CommentMessage(thread.Id, body, actorId);
            await _commentRepository.AddMessageAsync(message, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            return await GetThreadDtoOrThrowAsync(documentId, threadId, ct);
        }, cancellationToken);
    }

    public Task<CommentThreadDto> ResolveThreadAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentComment,
                ct,
                shareToken);
            var thread = await GetThreadOrThrowAsync(documentId, threadId, ct);

            thread.Resolve();
            await _unitOfWork.SaveChangesAsync(ct);

            return await GetThreadDtoOrThrowAsync(documentId, threadId, ct);
        }, cancellationToken);
    }

    public Task<CommentThreadDto> ReopenThreadAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentComment,
                ct,
                shareToken);
            var thread = await GetThreadOrThrowAsync(documentId, threadId, ct);

            thread.Reopen();
            await _unitOfWork.SaveChangesAsync(ct);

            return await GetThreadDtoOrThrowAsync(documentId, threadId, ct);
        }, cancellationToken);
    }

    private async Task<CommentThread> GetThreadOrThrowAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken)
    {
        return await _commentRepository.GetThreadAsync(documentId, threadId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Comment thread was not found.");
    }

    private async Task<CommentThreadDto> GetThreadDtoOrThrowAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken)
    {
        return await _commentRepository.GetThreadDtoAsync(documentId, threadId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Comment thread was not found.");
    }

    private static string NormalizeBody(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Comment body must not be empty.");
        }

        return body;
    }

    private static string NormalizeAnchorJson(Guid documentId, JsonElement anchor)
    {
        EnsureValidAnchor(documentId, anchor);
        return JsonSerializer.Serialize(anchor);
    }

    private static void EnsureValidAnchor(Guid documentId, JsonElement anchor)
    {
        if (anchor.ValueKind != JsonValueKind.Object)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "anchor must be a JSON object.");
        }

        RequireString(anchor, "schema", AnchorSchema);
        RequireString(anchor, "kind", TextRangeKind);

        if (!anchor.TryGetProperty("documentId", out var anchorDocumentId) ||
            anchorDocumentId.ValueKind != JsonValueKind.String ||
            string.IsNullOrWhiteSpace(anchorDocumentId.GetString()))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "anchor.documentId is required.");
        }

        if (Guid.TryParse(anchorDocumentId.GetString(), out var parsedDocumentId) &&
            parsedDocumentId != documentId)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "anchor.documentId must match documentId.");
        }

        RequireObject(anchor, "pm");
        RequireObject(anchor, "quote");
        RequireObject(anchor, "display");
    }

    private static void RequireString(JsonElement anchor, string propertyName, string expectedValue)
    {
        if (!anchor.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.String ||
            !string.Equals(property.GetString(), expectedValue, StringComparison.Ordinal))
        {
            throw new ApplicationErrorException(
                ErrorCodes.ValidationError,
                $"anchor.{propertyName} must be {expectedValue}.");
        }
    }

    private static void RequireObject(JsonElement anchor, string propertyName)
    {
        if (!anchor.TryGetProperty(propertyName, out var property) ||
            property.ValueKind != JsonValueKind.Object)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"anchor.{propertyName} is required.");
        }
    }
}
