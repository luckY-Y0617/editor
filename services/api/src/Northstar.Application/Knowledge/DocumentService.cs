using System.Text;
using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Application.Files;
using Northstar.Application.Security;
using Northstar.Contracts.Common;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Documents;
using Northstar.Domain.Knowledge.Versions;
using Northstar.Domain.Security;
using Northstar.Domain.Shared;

namespace Northstar.Application.Knowledge;

public sealed class DocumentService : IDocumentService
{
    private const string DefaultTitle = "Untitled Field Note";
    private const string CompareTargetCurrent = "current";
    private const string CompareTargetDraft = "draft";
    private const string CompareTargetVersion = "version";
    private const int ComparePreferredUnitLength = 96;
    private const int CompareHardUnitLength = 150;

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

    public async Task<DocumentVersionsResponse> GetVersionsAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.VersionView,
            cancellationToken);

        var versions = await _documentRepository.GetDocumentVersionsAsync(documentId, cancellationToken);
        return new DocumentVersionsResponse(versions.Select(MapVersionSummary).ToArray());
    }

    public async Task<DocumentVersionResponse> GetVersionAsync(
        Guid documentId,
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.VersionView,
            cancellationToken);

        var version = await _documentRepository.GetDocumentVersionAsync(documentId, versionId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document version was not found.");

        return new DocumentVersionResponse(
            MapVersionSummary(version),
            ParseJson(version.Content),
            ParseJson(version.Outline));
    }

    public Task<PublishDocumentVersionResponse> PublishVersionAsync(
        Guid documentId,
        PublishDocumentVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.VersionCreate,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (request.BaseRevision != editState.Document.Revision)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Document revision conflict.");
            }

            var versionNo = await _documentRepository.GetNextDocumentVersionNoAsync(documentId, ct);
            var label = NormalizeVersionLabel(request.Label, versionNo);
            if (await _documentRepository.DocumentVersionLabelExistsAsync(documentId, label, ct))
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Document version label already exists.");
            }

            var version = new DocumentVersion(
                editState.Document.WorkspaceId,
                editState.Document.Id,
                versionNo,
                label,
                DocumentVersionType.Published,
                editState.Draft.Content,
                editState.Draft.TextContent,
                editState.Draft.Outline,
                editState.Draft.WordCount,
                actorId);

            await _documentRepository.AddDocumentVersionAsync(version, ct);
            editState.Document.MarkPublished(version.Id, actorId);
            await _derivedDataWriter.RecordDocumentVersionPublishedAsync(
                editState.Document,
                editState.Draft,
                version,
                actorId,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);

            var document = await _queryService.GetDocumentAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");

            return new PublishDocumentVersionResponse(document, MapVersionSummary(version));
        }, cancellationToken);
    }

    public Task<RestoreDocumentVersionResponse> RestoreVersionAsync(
        Guid documentId,
        Guid versionId,
        RestoreDocumentVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.VersionRestore,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (request.BaseRevision != editState.Document.Revision)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Document revision conflict.");
            }

            if (editState.Document.Status == DocumentStatus.Archived)
            {
                throw new ApplicationErrorException(ErrorCodes.ValidationError, "Archived documents cannot restore versions.");
            }

            var version = await _documentRepository.GetDocumentVersionAsync(documentId, versionId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document version was not found.");

            using var contentDocument = JsonDocument.Parse(version.Content);
            var metadata = DocumentContentAnalyzer.Analyze(contentDocument.RootElement);
            editState.Draft.UpdateContent(
                metadata.ContentJson,
                metadata.TextContent,
                metadata.OutlineJson,
                metadata.WordCount,
                metadata.ContentHash,
                actorId);
            editState.Document.MarkDraftAfterVersionedChange(actorId);
            editState.Document.IncrementRevision(actorId);
            await _derivedDataWriter.RecordDocumentVersionRestoredAsync(
                editState.Document,
                editState.Draft,
                version,
                actorId,
                ct);

            await _unitOfWork.SaveChangesAsync(ct);

            var document = await _queryService.GetDocumentAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");

            return new RestoreDocumentVersionResponse(document, MapVersionSummary(version));
        }, cancellationToken);
    }

    public Task<UnpublishDocumentVersionResponse> UnpublishVersionAsync(
        Guid documentId,
        UnpublishDocumentVersionRequest request,
        CancellationToken cancellationToken = default)
    {
        return _transactionRunner.ExecuteAsync(async ct =>
        {
            var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
            await _scopedAccessService.EnsureCanAccessDocumentAsync(
                documentId,
                PermissionActions.VersionCreate,
                ct);
            var actorId = await _scopedAccessService.GetRequiredUserIdAsync(ct);

            if (request.BaseRevision != editState.Document.Revision)
            {
                throw new ApplicationErrorException(ErrorCodes.Conflict, "Document revision conflict.");
            }

            DocumentVersion? previousVersion = null;
            if (editState.Document.CurrentPublishedVersionId.HasValue)
            {
                previousVersion = await _documentRepository.GetDocumentVersionAsync(
                    documentId,
                    editState.Document.CurrentPublishedVersionId.Value,
                    ct);
            }

            if (editState.Document.MarkUnpublished(actorId))
            {
                await _derivedDataWriter.RecordDocumentVersionUnpublishedAsync(
                    editState.Document,
                    editState.Draft,
                    previousVersion,
                    actorId,
                    ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }

            var document = await _queryService.GetDocumentAsync(documentId, ct)
                ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");

            return new UnpublishDocumentVersionResponse(
                document,
                previousVersion is null ? null : MapVersionSummary(previousVersion));
        }, cancellationToken);
    }

    public async Task<CompareDocumentVersionsResponse> CompareVersionsAsync(
        Guid documentId,
        CompareDocumentVersionsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.From is null || request.To is null)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Both compare targets are required.");
        }

        await _scopedAccessService.EnsureCanAccessDocumentAsync(
            documentId,
            PermissionActions.VersionView,
            cancellationToken);

        var editState = await _documentRepository.GetDocumentEditStateAsync(documentId, cancellationToken: cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document was not found.");
        var from = await ResolveCompareTargetAsync(editState, request.From, cancellationToken);
        var to = await ResolveCompareTargetAsync(editState, request.To, cancellationToken);
        var segments = BuildTextDiffSegments(from.TextContent, to.TextContent);
        var lines = BuildTextDiffLines(segments);

        return new CompareDocumentVersionsResponse(
            NormalizeCompareTarget(request.From),
            NormalizeCompareTarget(request.To),
            new DocumentVersionCompareSummaryDto(
                from.Label,
                to.Label,
                !string.Equals(from.TextContent, to.TextContent, StringComparison.Ordinal),
                segments.Count(segment => segment.Kind == "added"),
                segments.Count(segment => segment.Kind == "removed"),
                to.WordCount - from.WordCount),
            segments,
            lines);
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

            editState.Document.MarkDraftAfterVersionedChange(actorId);
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

    private static string NormalizeVersionLabel(string? label, int versionNo)
    {
        label = label?.Trim();
        return string.IsNullOrWhiteSpace(label) ? $"{versionNo}.0" : label;
    }

    private async Task<VersionCompareSnapshot> ResolveCompareTargetAsync(
        DocumentEditState editState,
        DocumentVersionCompareTargetDto target,
        CancellationToken cancellationToken)
    {
        var type = NormalizeCompareTargetType(target.Type);
        if (type is CompareTargetCurrent or CompareTargetDraft)
        {
            return new VersionCompareSnapshot("Current draft", editState.Draft.TextContent, editState.Draft.WordCount);
        }

        if (type != CompareTargetVersion)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Compare target type must be version or draft.");
        }

        if (!Guid.TryParse(target.VersionId, out var versionId))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "versionId must be a valid UUID for version compare targets.");
        }

        var version = await _documentRepository.GetDocumentVersionAsync(editState.Document.Id, versionId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.NotFound, "Document version was not found.");

        return new VersionCompareSnapshot(version.Label, version.TextContent, version.WordCount);
    }

    private static DocumentVersionCompareTargetDto NormalizeCompareTarget(DocumentVersionCompareTargetDto target)
    {
        var type = NormalizeCompareTargetType(target.Type);
        return new DocumentVersionCompareTargetDto(
            type == CompareTargetCurrent ? CompareTargetDraft : type,
            type == CompareTargetVersion ? target.VersionId : null);
    }

    private static string NormalizeCompareTargetType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            CompareTargetCurrent => CompareTargetCurrent,
            CompareTargetDraft => CompareTargetDraft,
            CompareTargetVersion => CompareTargetVersion,
            _ => string.Empty
        };
    }

    private static IReadOnlyList<DocumentVersionCompareSegmentDto> BuildTextDiffSegments(
        string fromText,
        string toText)
    {
        var fromSegments = SplitCompareText(fromText);
        var toSegments = SplitCompareText(toText);
        var lcs = new int[fromSegments.Length + 1, toSegments.Length + 1];

        for (var fromIndex = fromSegments.Length - 1; fromIndex >= 0; fromIndex--)
        {
            for (var toIndex = toSegments.Length - 1; toIndex >= 0; toIndex--)
            {
                lcs[fromIndex, toIndex] = string.Equals(fromSegments[fromIndex], toSegments[toIndex], StringComparison.Ordinal)
                    ? lcs[fromIndex + 1, toIndex + 1] + 1
                    : Math.Max(lcs[fromIndex + 1, toIndex], lcs[fromIndex, toIndex + 1]);
            }
        }

        var result = new List<DocumentVersionCompareSegmentDto>();
        var left = 0;
        var right = 0;
        while (left < fromSegments.Length && right < toSegments.Length)
        {
            if (string.Equals(fromSegments[left], toSegments[right], StringComparison.Ordinal))
            {
                result.Add(new DocumentVersionCompareSegmentDto("unchanged", fromSegments[left]));
                left++;
                right++;
                continue;
            }

            if (lcs[left + 1, right] >= lcs[left, right + 1])
            {
                result.Add(new DocumentVersionCompareSegmentDto("removed", fromSegments[left]));
                left++;
            }
            else
            {
                result.Add(new DocumentVersionCompareSegmentDto("added", toSegments[right]));
                right++;
            }
        }

        while (left < fromSegments.Length)
        {
            result.Add(new DocumentVersionCompareSegmentDto("removed", fromSegments[left]));
            left++;
        }

        while (right < toSegments.Length)
        {
            result.Add(new DocumentVersionCompareSegmentDto("added", toSegments[right]));
            right++;
        }

        return result;
    }

    private static string[] SplitCompareText(string value)
    {
        var normalized = NormalizeCompareText(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        return normalized
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .SelectMany(SplitCompareLine)
            .Where(segment => !string.IsNullOrWhiteSpace(segment))
            .ToArray();
    }

    private static string NormalizeCompareText(string value)
    {
        var normalizedNewlines = value
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Replace('\t', ' ')
            .Replace('\u3000', ' ');
        var lines = normalizedNewlines
            .Split('\n')
            .Select(NormalizeInlineWhitespace)
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join('\n', lines);
    }

    private static string NormalizeInlineWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasWhitespace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsWhiteSpace(character))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                }

                previousWasWhitespace = true;
                continue;
            }

            builder.Append(character);
            previousWasWhitespace = false;
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitCompareLine(string line)
    {
        var current = new StringBuilder();

        for (var index = 0; index < line.Length; index++)
        {
            current.Append(line[index]);
            if (ShouldSplitCompareUnit(line, index, current.Length))
            {
                yield return current.ToString().Trim();
                current.Clear();
            }
        }

        if (current.Length > 0)
        {
            yield return current.ToString().Trim();
        }
    }

    private static bool ShouldSplitCompareUnit(string line, int index, int unitLength)
    {
        if (unitLength <= 0)
        {
            return false;
        }

        if (IsStrongCompareBreak(line, index))
        {
            return true;
        }

        if (unitLength >= ComparePreferredUnitLength && IsSoftCompareBreak(line[index]))
        {
            return true;
        }

        return unitLength >= CompareHardUnitLength;
    }

    private static bool IsStrongCompareBreak(string line, int index)
    {
        var character = line[index];
        if (character is '。' or '！' or '？' or '；' or '!' or '?' or ';')
        {
            return true;
        }

        if (character != '.')
        {
            return false;
        }

        var previousIsDigit = index > 0 && char.IsDigit(line[index - 1]);
        var nextIsDigit = index + 1 < line.Length && char.IsDigit(line[index + 1]);
        if (previousIsDigit && nextIsDigit)
        {
            return false;
        }

        return index + 1 == line.Length || char.IsWhiteSpace(line[index + 1]);
    }

    private static bool IsSoftCompareBreak(char character)
    {
        return character is '，' or ',' or '、' or '：' or ':' or ')' or '）';
    }

    private static IReadOnlyList<DocumentVersionCompareLineDto> BuildTextDiffLines(
        IReadOnlyList<DocumentVersionCompareSegmentDto> segments)
    {
        var lines = new List<DocumentVersionCompareLineDto>();

        for (var index = 0; index < segments.Count;)
        {
            var segment = segments[index];
            if (segment.Kind == "unchanged")
            {
                lines.Add(CreateUnchangedCompareLine(segment.Text));
                index++;
                continue;
            }

            if (segment.Kind == "removed")
            {
                var removed = CollectAdjacentSegments(segments, ref index, "removed");
                var added = index < segments.Count && segments[index].Kind == "added"
                    ? CollectAdjacentSegments(segments, ref index, "added")
                    : [];
                AddPairedCompareLines(lines, removed, added);
                continue;
            }

            if (segment.Kind == "added")
            {
                var added = CollectAdjacentSegments(segments, ref index, "added");
                var removed = index < segments.Count && segments[index].Kind == "removed"
                    ? CollectAdjacentSegments(segments, ref index, "removed")
                    : [];
                AddPairedCompareLines(lines, removed, added);
                continue;
            }

            lines.Add(CreateUnchangedCompareLine(segment.Text));
            index++;
        }

        return lines;
    }

    private static IReadOnlyList<string> CollectAdjacentSegments(
        IReadOnlyList<DocumentVersionCompareSegmentDto> segments,
        ref int index,
        string kind)
    {
        var collected = new List<string>();

        while (index < segments.Count && segments[index].Kind == kind)
        {
            collected.Add(segments[index].Text);
            index++;
        }

        return collected;
    }

    private static void AddPairedCompareLines(
        List<DocumentVersionCompareLineDto> lines,
        IReadOnlyList<string> removed,
        IReadOnlyList<string> added)
    {
        var count = Math.Max(removed.Count, added.Count);

        for (var index = 0; index < count; index++)
        {
            var left = index < removed.Count ? removed[index] : null;
            var right = index < added.Count ? added[index] : null;

            if (!string.IsNullOrWhiteSpace(left) && !string.IsNullOrWhiteSpace(right))
            {
                lines.Add(new DocumentVersionCompareLineDto(
                    "modified",
                    left,
                    right,
                    BuildInlineDiffTokens(left, right, "removed"),
                    BuildInlineDiffTokens(left, right, "added")));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(left))
            {
                lines.Add(new DocumentVersionCompareLineDto(
                    "removed",
                    left,
                    null,
                    [new DocumentVersionCompareTokenDto("removed", left)],
                    []));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(right))
            {
                lines.Add(new DocumentVersionCompareLineDto(
                    "added",
                    null,
                    right,
                    [],
                    [new DocumentVersionCompareTokenDto("added", right)]));
            }
        }
    }

    private static DocumentVersionCompareLineDto CreateUnchangedCompareLine(string text)
    {
        return new DocumentVersionCompareLineDto(
            "unchanged",
            text,
            text,
            [new DocumentVersionCompareTokenDto("unchanged", text)],
            [new DocumentVersionCompareTokenDto("unchanged", text)]);
    }

    private static IReadOnlyList<DocumentVersionCompareTokenDto> BuildInlineDiffTokens(
        string fromText,
        string toText,
        string changedKind)
    {
        var fromTokens = TokenizeInlineCompareText(fromText);
        var toTokens = TokenizeInlineCompareText(toText);
        var lcs = new int[fromTokens.Length + 1, toTokens.Length + 1];

        for (var fromIndex = fromTokens.Length - 1; fromIndex >= 0; fromIndex--)
        {
            for (var toIndex = toTokens.Length - 1; toIndex >= 0; toIndex--)
            {
                lcs[fromIndex, toIndex] = string.Equals(fromTokens[fromIndex], toTokens[toIndex], StringComparison.Ordinal)
                    ? lcs[fromIndex + 1, toIndex + 1] + 1
                    : Math.Max(lcs[fromIndex + 1, toIndex], lcs[fromIndex, toIndex + 1]);
            }
        }

        var tokens = new List<DocumentVersionCompareTokenDto>();
        var left = 0;
        var right = 0;
        while (left < fromTokens.Length && right < toTokens.Length)
        {
            if (string.Equals(fromTokens[left], toTokens[right], StringComparison.Ordinal))
            {
                AppendCompareToken(tokens, "unchanged", fromTokens[left]);
                left++;
                right++;
                continue;
            }

            if (lcs[left + 1, right] >= lcs[left, right + 1])
            {
                if (changedKind == "removed")
                {
                    AppendCompareToken(tokens, "removed", fromTokens[left]);
                }

                left++;
            }
            else
            {
                if (changedKind == "added")
                {
                    AppendCompareToken(tokens, "added", toTokens[right]);
                }

                right++;
            }
        }

        while (left < fromTokens.Length)
        {
            if (changedKind == "removed")
            {
                AppendCompareToken(tokens, "removed", fromTokens[left]);
            }

            left++;
        }

        while (right < toTokens.Length)
        {
            if (changedKind == "added")
            {
                AppendCompareToken(tokens, "added", toTokens[right]);
            }

            right++;
        }

        return tokens;
    }

    private static string[] TokenizeInlineCompareText(string text)
    {
        var normalized = NormalizeInlineWhitespace(text);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return [];
        }

        var tokens = new List<string>();
        var current = new StringBuilder();

        foreach (var character in normalized)
        {
            if (char.IsWhiteSpace(character))
            {
                FlushInlineToken(tokens, current);
                tokens.Add(" ");
                continue;
            }

            if (IsCjkCharacter(character) || char.IsPunctuation(character) || char.IsSymbol(character))
            {
                FlushInlineToken(tokens, current);
                tokens.Add(character.ToString());
                continue;
            }

            current.Append(character);
        }

        FlushInlineToken(tokens, current);
        return tokens.ToArray();
    }

    private static void FlushInlineToken(List<string> tokens, StringBuilder current)
    {
        if (current.Length == 0)
        {
            return;
        }

        tokens.Add(current.ToString());
        current.Clear();
    }

    private static bool IsCjkCharacter(char character)
    {
        return character is >= '\u3400' and <= '\u4dbf'
            or >= '\u4e00' and <= '\u9fff'
            or >= '\uf900' and <= '\ufaff';
    }

    private static void AppendCompareToken(
        List<DocumentVersionCompareTokenDto> tokens,
        string kind,
        string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        if (tokens.Count > 0 && tokens[^1].Kind == kind)
        {
            tokens[^1] = tokens[^1] with { Text = tokens[^1].Text + text };
            return;
        }

        tokens.Add(new DocumentVersionCompareTokenDto(kind, text));
    }

    private static DocumentVersionSummaryDto MapVersionSummary(DocumentVersion version)
    {
        return new DocumentVersionSummaryDto(
            version.Id.ToString(),
            version.DocumentId.ToString(),
            version.VersionNo,
            version.Label,
            version.VersionType,
            version.CreatedAt,
            version.PublishedAt,
            version.CreatedBy?.ToString(),
            version.WordCount);
    }

    private sealed record VersionCompareSnapshot(string Label, string TextContent, int WordCount);

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
