using System.Text.Json;

namespace Northstar.Contracts.Knowledge;

public sealed record WorkspaceExportDto(string Id, string Name);

public sealed record SpaceExportDto(string Id, string Name);

public sealed record CollectionExportDto(
    string Id,
    string Title,
    decimal SortOrder);

public sealed record DocumentExportDto(
    string Id,
    string FolderId,
    string Title,
    string Status,
    decimal SortOrder,
    IReadOnlyList<string> Tags,
    JsonElement Content,
    long Revision,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record ExportSpaceResponse(
    string SchemaVersion,
    DateTimeOffset ExportedAt,
    WorkspaceExportDto Workspace,
    SpaceExportDto Space,
    IReadOnlyList<CollectionExportDto> Collections,
    IReadOnlyList<DocumentExportDto> Documents);

public sealed record CollectionImportDto(
    string? Id,
    string Title,
    decimal? SortOrder);

public sealed record DocumentImportDto(
    string? Id,
    string? FolderId,
    string Title,
    string? Status,
    decimal? SortOrder,
    IReadOnlyList<string>? Tags,
    JsonElement Content);

public sealed record ImportSpaceRequest(
    string Mode,
    IReadOnlyList<CollectionImportDto>? Collections,
    IReadOnlyList<DocumentImportDto> Documents);

public sealed record ImportSpaceResponse(
    int ImportedCollectionCount,
    int ImportedDocumentCount,
    KnowledgeMapResponse Map);
