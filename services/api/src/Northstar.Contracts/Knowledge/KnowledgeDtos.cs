using System.Text.Json;

namespace Northstar.Contracts.Knowledge;

public sealed record WorkspaceDto(string Id, string Name, string CurrentSpaceId, string OrganizationId);

public sealed record SpaceDto(string Id, string Name);

public sealed record KnowledgeFolderDto(
    string Id,
    string Title,
    decimal SortOrder,
    int DocumentCount);

public sealed record KnowledgeDocumentSummaryDto(
    string Id,
    string FolderId,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    decimal SortOrder);

public sealed record OwnerDto(string Id, string Name);

public sealed record KnowledgeDocumentDto(
    string Id,
    string FolderId,
    string Title,
    string Status,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    decimal SortOrder,
    OwnerDto Owner,
    string Version,
    JsonElement Content,
    long Revision);

public sealed record KnowledgeMapResponse(
    IReadOnlyList<KnowledgeFolderDto> Folders,
    IReadOnlyList<KnowledgeDocumentSummaryDto> Documents);

public sealed record BootstrapResponse(
    WorkspaceDto Workspace,
    IReadOnlyList<SpaceDto> Spaces,
    string ActiveSpaceId,
    IReadOnlyList<KnowledgeFolderDto> Folders,
    IReadOnlyList<KnowledgeDocumentSummaryDto> Documents,
    string ActiveDocumentId);

public sealed record CreateDocumentRequest(string FolderId, string? Title);

public sealed record CreateDocumentResponse(
    KnowledgeDocumentDto Document,
    KnowledgeMapResponse Map);

public sealed record GetDocumentResponse(KnowledgeDocumentDto Document);

public sealed record UpdateDocumentRequest(
    long BaseRevision,
    string? Title,
    JsonElement? Content,
    IReadOnlyList<string>? Tags);

public sealed record UpdateDocumentResponse(KnowledgeDocumentDto Document);

public sealed record MoveDocumentRequest(string FolderId, decimal? SortOrder);

public sealed record MoveDocumentResponse(
    KnowledgeDocumentSummaryDto Document,
    KnowledgeMapResponse Map);

public sealed record DocumentVersionSummaryDto(
    string Id,
    string DocumentId,
    int VersionNo,
    string Label,
    string VersionType,
    DateTimeOffset CreatedAt,
    DateTimeOffset? PublishedAt,
    string? CreatedBy,
    int WordCount);

public sealed record DocumentVersionsResponse(IReadOnlyList<DocumentVersionSummaryDto> Versions);

public sealed record DocumentVersionResponse(
    DocumentVersionSummaryDto Version,
    JsonElement Content,
    JsonElement Outline);

public sealed record PublishDocumentVersionRequest(long BaseRevision, string? Label);

public sealed record PublishDocumentVersionResponse(
    KnowledgeDocumentDto Document,
    DocumentVersionSummaryDto Version);

public sealed record UnpublishDocumentVersionRequest(long BaseRevision);

public sealed record UnpublishDocumentVersionResponse(
    KnowledgeDocumentDto Document,
    DocumentVersionSummaryDto? UnpublishedVersion);

public sealed record RestoreDocumentVersionRequest(long BaseRevision);

public sealed record RestoreDocumentVersionResponse(
    KnowledgeDocumentDto Document,
    DocumentVersionSummaryDto RestoredFrom);

public sealed record DocumentVersionCompareTargetDto(string Type, string? VersionId);

public sealed record CompareDocumentVersionsRequest(
    DocumentVersionCompareTargetDto From,
    DocumentVersionCompareTargetDto To);

public sealed record DocumentVersionCompareSummaryDto(
    string FromLabel,
    string ToLabel,
    bool TextChanged,
    int AddedSegments,
    int RemovedSegments,
    int WordCountDelta);

public sealed record DocumentVersionCompareSegmentDto(string Kind, string Text);

public sealed record DocumentVersionCompareTokenDto(string Kind, string Text);

public sealed record DocumentVersionCompareLineDto(
    string Kind,
    string? LeftText,
    string? RightText,
    IReadOnlyList<DocumentVersionCompareTokenDto> LeftTokens,
    IReadOnlyList<DocumentVersionCompareTokenDto> RightTokens);

public sealed record CompareDocumentVersionsResponse(
    DocumentVersionCompareTargetDto From,
    DocumentVersionCompareTargetDto To,
    DocumentVersionCompareSummaryDto Summary,
    IReadOnlyList<DocumentVersionCompareSegmentDto> Segments,
    IReadOnlyList<DocumentVersionCompareLineDto> Lines);

public sealed record CreateCollectionRequest(string Title, decimal? SortOrder);

public sealed record UpdateCollectionRequest(string? Title, decimal? SortOrder);

public sealed record ReorderCollectionsRequest(IReadOnlyList<string> CollectionIds);

public sealed record CollectionMutationResponse(
    KnowledgeFolderDto Collection,
    KnowledgeMapResponse Map);
