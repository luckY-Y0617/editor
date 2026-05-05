using System.Text.Json;

namespace Northstar.Contracts.Security;

public sealed record ScimServiceProviderConfigResponse(
    IReadOnlyList<string> Schemas,
    string DocumentationUri,
    ScimSupportedFeature Patch,
    ScimBulkFeature Bulk,
    ScimFilterFeature Filter,
    ScimSupportedFeature ChangePassword,
    ScimSupportedFeature Sort,
    ScimSupportedFeature Etag,
    IReadOnlyList<ScimAuthenticationSchemeDto> AuthenticationSchemes);

public sealed record ScimSupportedFeature(bool Supported);

public sealed record ScimBulkFeature(bool Supported, int MaxOperations, int MaxPayloadSize);

public sealed record ScimFilterFeature(bool Supported, int MaxResults);

public sealed record ScimAuthenticationSchemeDto(
    string Name,
    string Description,
    string Type,
    bool Primary);

public sealed record ScimListResponse<TResource>(
    IReadOnlyList<string> Schemas,
    int TotalResults,
    int ItemsPerPage,
    int StartIndex,
    IReadOnlyList<TResource> Resources);

public sealed record ScimResourceTypeDto(
    string Id,
    string Name,
    string Endpoint,
    string Description,
    string Schema,
    IReadOnlyList<ScimSchemaExtensionDto> SchemaExtensions);

public sealed record ScimSchemaExtensionDto(
    string Schema,
    bool Required);

public sealed record ScimSchemaDto(
    IReadOnlyList<string> Schemas,
    string Id,
    string Name,
    string Description,
    IReadOnlyList<ScimSchemaAttributeDto> Attributes);

public sealed record ScimSchemaAttributeDto(
    string Name,
    string Type,
    bool MultiValued,
    string Description,
    bool Required,
    string Mutability,
    string Returned,
    string Uniqueness);

public sealed record ScimMetaDto(
    string ResourceType,
    DateTimeOffset Created,
    DateTimeOffset LastModified,
    string Location);

public sealed record ScimNameDto(
    string? Formatted,
    string? GivenName,
    string? FamilyName);

public sealed record ScimUserResource(
    IReadOnlyList<string> Schemas,
    string Id,
    string? ExternalId,
    string UserName,
    string DisplayName,
    ScimNameDto? Name,
    bool Active,
    ScimMetaDto Meta);

public sealed record CreateScimUserRequest(
    string? UserName,
    string? ExternalId,
    string? DisplayName,
    ScimNameDto? Name,
    bool? Active);

public sealed record ScimGroupMemberDto(
    string Value,
    string? Display);

public sealed record ScimGroupResource(
    IReadOnlyList<string> Schemas,
    string Id,
    string? ExternalId,
    string DisplayName,
    IReadOnlyList<ScimGroupMemberDto> Members,
    ScimMetaDto Meta);

public sealed record CreateScimGroupRequest(
    string? DisplayName,
    string? ExternalId,
    IReadOnlyList<ScimGroupMemberDto>? Members);

public sealed record ScimPatchRequest(
    IReadOnlyList<string>? Schemas,
    IReadOnlyList<ScimPatchOperationDto>? Operations);

public sealed record ScimPatchOperationDto(
    string? Op,
    string? Path,
    JsonElement Value);

public sealed record CreateScimTokenRequest(
    string Name,
    DateTimeOffset? ExpiresAt);

public sealed record CreateScimTokenResponse(
    ScimTokenDto Token,
    string RawToken);

public sealed record ScimTokensResponse(
    IReadOnlyList<ScimTokenDto> Tokens);

public sealed record ScimTokenDto(
    string Id,
    string WorkspaceId,
    string Name,
    string? CreatedBy,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? RevokedAt,
    DateTimeOffset? LastUsedAt);
