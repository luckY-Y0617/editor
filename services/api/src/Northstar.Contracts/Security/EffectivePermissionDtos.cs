namespace Northstar.Contracts.Security;

public sealed record EffectivePermissionResponse(
    string ResourceType,
    string ResourceId,
    IReadOnlyList<string> AllowedActions,
    string? EffectiveRole,
    string Source,
    string InheritanceMode);
