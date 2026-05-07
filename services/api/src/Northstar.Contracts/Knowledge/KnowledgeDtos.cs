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

public sealed record CreateCollectionRequest(string Title, decimal? SortOrder);

public sealed record UpdateCollectionRequest(string? Title, decimal? SortOrder);

public sealed record ReorderCollectionsRequest(IReadOnlyList<string> CollectionIds);

public sealed record CollectionMutationResponse(
    KnowledgeFolderDto Collection,
    KnowledgeMapResponse Map);
