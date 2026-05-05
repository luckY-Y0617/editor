namespace Northstar.Contracts.Knowledge;

public sealed record SearchResponse(IReadOnlyList<SearchResultDto> Results);

public sealed record SearchResultDto(
    string Id,
    string Type,
    string Title,
    string FolderId,
    string Excerpt,
    DateTimeOffset UpdatedAt);

