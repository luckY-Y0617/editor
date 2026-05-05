using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Security;
using Northstar.Domain.Shared;

namespace Northstar.Application.Knowledge;

public sealed class DocumentService : IDocumentService
{
    private const string DefaultTitle = "Untitled Field Note";

    private readonly IDocumentRepository _documentRepository;
    private readonly IDocumentDerivedDataWriter _derivedDataWriter;
    private readonly IKnowledgeQueryService _queryService;
    private readonly IWorkspaceAccessService _accessService;
    private readonly IScopedResourceAccessService _scopedAccessService;
    private readonly IResourceWorkspaceResolver _workspaceResolver;
    private readonly ITransactionRunner _transactionRunner;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileReferenceService _fileReferenceService;

    public DocumentService(
        IDocumentRepository documentRepository,
        IDocumentDerivedDataWriter derivedDataWriter,
        IKnowledgeQueryService queryService,
        IWorkspaceAccessService accessService,
        IScopedResourceAccessService scopedAccessService,
        IResourceWorkspaceResolver workspaceResolver,
        ITransactionRunner transactionRunner,
        IUnitOfWork unitOfWork,
        IFileReferenceService fileReferenceService)
    {
        _documentRepository = documentRepository;
        _derivedDataWriter = derivedDataWriter;
        _queryService = queryService;
        _accessService = accessService;
        _scopedAccessService = scopedAccessService;
        _workspaceResolver = workspaceResolver;
        _transactionRunner = transactionRunner;
        _unitOfWork = unitOfWork;
        _fileReferenceService = fileReferenceService;
    }

    public Task<CreateDocumentResponse> CreateAsync(
        CreateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var folderId = ParseId(request.FolderId, "folderId");
        var title = string.IsNullOrWhiteSpace(request.Title) ? DefaultTitle : request.Title.Trim();

        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var location = await _documentRepository.GetCollectionLocationAsync(folderId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Folder was not found.");
            await _scopedAccessService.EnsureCanAccessCollectionAsync(
                location.CollectionId,
                PermissionActions.CollectionCreateDocument,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            var sortOrder = await _documentRepository.GetNextDocumentSortOrderAsync(folderId, ct);
            var document = new Document(
                location.WorkspaceId,
                location.SpaceId,
                location.CollectionId,
                title,
                actorId,
                actorId,
                sortOrder: sortOrder);
            var draft = new DocumentDraft(document.Id, document.WorkspaceId, JsonDefaults.EmptyTiptapDocument, actorId);
            using var contentDocument = JsonDocument.Parse(JsonDefaults.EmptyTiptapDocument);
            var metadata = DocumentContentAnalyzer.Analyze(contentDocument.RootElement);
            draft.UpdateContent(
                metadata.ContentJson,
                metadata.TextContent,
                metadata.OutlineJson,
                metadata.WordCount,
                metadata.ContentHash,
                actorId);

            await _documentRepository.AddDocumentAsync(document, draft, ct);
            await _derivedDataWriter.RecordDocumentCreatedAsync(document, draft, actorId, ct);
            await _unitOfWork.SaveChangesAsync(ct);

            var documentDto = await _queryService.GetDocumentAsync(document.Id, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found after creation.");
            var map = await _queryService.GetMapAsync(location.SpaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");

            return new CreateDocumentResponse(documentDto, map);
        }, cancellationToken);
    }

    public async Task<GetDocumentResponse> GetAsync(
        Guid documentId,
        CancellationToken cancellationToken = default,
        string? shareToken = null)
    {
        var workspaceId = await _workspaceResolver.GetWorkspaceIdForDocumentAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.DocumentView,
            cancellationToken,
            shareToken);

        var document = await _queryService.GetDocumentAsync(documentId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");

        return new GetDocumentResponse(document);
    }

    public Task<UpdateDocumentResponse> UpdateAsync(
        Guid documentId,
        UpdateDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentEdit,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (request.BaseRevision != editState.Document.Revision)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Document revision conflict.");
            }

            var changedFields = new List<string>();
            var contentChanged = false;
            var fileReferencesChanged = false;

            if (request.Title is not null)
            {
                var title = request.Title.Trim();
                if (!string.Equals(title, editState.Document.Title, StringComparison.Ordinal))
                {
                    editState.Document.Rename(request.Title, actorId);
                    changedFields.Add("title");
                }
            }

            if (request.Content.HasValue)
            {
                EnsureJsonObject(request.Content.Value, "content");
                fileReferencesChanged = await _fileReferenceService.ValidateAndSyncDocumentReferencesAsync(
                    editState.Document.Id,
                    editState.Document.WorkspaceId,
                    request.Content.Value,
                    actorId,
                    ct);
                var metadata = DocumentContentAnalyzer.Analyze(request.Content.Value);
                if (!string.Equals(metadata.ContentHash, editState.Draft.ContentHash, StringComparison.Ordinal))
                {
                    editState.Draft.UpdateContent(
                        metadata.ContentJson,
                        metadata.TextContent,
                        metadata.OutlineJson,
                        metadata.WordCount,
                        metadata.ContentHash,
                        actorId);
                    changedFields.Add("content");
                    contentChanged = true;
                }
            }

            if (request.Tags is not null)
            {
                var requestedTags = NormalizeTags(request.Tags);
                var currentTags = await _documentRepository.GetDocumentTagNamesAsync(editState.Document.Id, ct);
                if (!TagSetsEqual(requestedTags, currentTags))
                {
                    await _documentRepository.ReplaceDocumentTagsAsync(
                        editState.Document.WorkspaceId,
                        editState.Document.Id,
                        requestedTags,
                        actorId,
                        ct);
                    changedFields.Add("tags");
                }
            }

            if (changedFields.Count == 0)
            {
                if (fileReferencesChanged)
                {
                    await _unitOfWork.SaveChangesAsync(ct);
                }

                var currentDocument = await _queryService.GetDocumentAsync(documentId, ct)
                    ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
                return new UpdateDocumentResponse(currentDocument);
            }

            editState.Document.IncrementRevision(actorId);
            await _derivedDataWriter.RecordDocumentUpdatedAsync(
                editState.Document,
                editState.Draft,
                changedFields,
                contentChanged,
                actorId,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);

            var document = await _queryService.GetDocumentAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");

            return new UpdateDocumentResponse(document);
        }, cancellationToken);
    }

    public Task<MoveDocumentResponse> MoveAsync(
        Guid documentId,
        MoveDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        var folderId = ParseId(request.FolderId, "folderId");

        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var location = await _documentRepository.GetCollectionLocationAsync(folderId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Folder was not found.");
            await _scopedAccessService.EnsureCanAccessCollectionAsync(
                location.CollectionId,
                PermissionActions.CollectionCreateDocument,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentEdit,
                ct);

            if (editState.Document.WorkspaceId != location.WorkspaceId ||
                editState.Document.SpaceId != location.SpaceId)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Document cannot be moved across spaces in Phase 2.");
            }

            var oldCollectionId = editState.Document.CollectionId;
            editState.Document.Move(location.CollectionId, request.SortOrder, actorId);
            await _derivedDataWriter.RecordDocumentMovedAsync(
                editState.Document,
                editState.Draft,
                oldCollectionId,
                actorId,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);

            var document = await _queryService.GetDocumentSummaryAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            var map = await _queryService.GetMapAsync(location.SpaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");

            return new MoveDocumentResponse(document, map);
        }, cancellationToken);
    }

    public Task<MoveDocumentResponse> ArchiveAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentArchive,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (editState.Document.Archive(actorId))
            {
                await _derivedDataWriter.RecordDocumentArchivedAsync(editState.Document, actorId, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            var document = await _queryService.GetDocumentSummaryAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            var map = await _queryService.GetMapAsync(editState.Document.SpaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");

            return new MoveDocumentResponse(document, map);
        }, cancellationToken);
    }

    public Task<MoveDocumentResponse> RestoreAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.DocumentRestore,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (editState.Document.Restore(actorId))
            {
                await _derivedDataWriter.RecordDocumentRestoredAsync(editState.Document, editState.Draft, actorId, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            var document = await _queryService.GetDocumentSummaryAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            var map = await _queryService.GetMapAsync(editState.Document.SpaceId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Space was not found.");

            return new MoveDocumentResponse(document, map);
        }, cancellationToken);
    }

    public Task DeleteAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(
                    documentId,
                    includeDeleted: true,
                    cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentIncludingDeletedAsync(
                documentId,
                PermissionActions.DocumentDelete,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (!editState.Document.Delete(actorId))
            {
                return true;
            }

            await _derivedDataWriter.RecordDocumentDeletedAsync(editState.Document, actorId, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    private static Guid ParseId(string value, string fieldName)
    {
        return Guid.TryParse(value, out var id)
            ? id
            : throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a valid UUID.");
    }

    private static void EnsureJsonObject(JsonElement content, string fieldName)
    {
        if (content.ValueKind != JsonValueKind.Object)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, $"{fieldName} must be a JSON object.");
        }
    }

    private static IReadOnlyCollection<string> NormalizeTags(IReadOnlyList<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static bool TagSetsEqual(
        IReadOnlyCollection<string> requestedTags,
        IReadOnlyCollection<string> currentTags)
    {
        return requestedTags
            .Order(StringComparer.OrdinalIgnoreCase)
            .SequenceEqual(currentTags.Order(StringComparer.OrdinalIgnoreCase), StringComparer.OrdinalIgnoreCase);
    }
}
