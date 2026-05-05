namespace Northstar.Contracts.Knowledge;

public sealed record RelatedDocumentDto(string Id, string Code, string Title);

public sealed record VersionTrailItemDto(
    string Id,
    string Version,
    DateTimeOffset Date,
    string Author,
    string Status);

public sealed record BacklinkItemDto(
    string Id,
    string Code,
    string Title,
    string Excerpt);

public sealed record DocumentContextResponse(
    IReadOnlyList<RelatedDocumentDto> RelatedDocuments,
    IReadOnlyList<VersionTrailItemDto> VersionTrail,
    IReadOnlyList<BacklinkItemDto> Backlinks);

public sealed record ActivityTimelineItemDto(
    string Id,
    string Title,
    DateTimeOffset Date,
    string Detail);

public sealed record DocumentActivityResponse(IReadOnlyList<ActivityTimelineItemDto> Items);

