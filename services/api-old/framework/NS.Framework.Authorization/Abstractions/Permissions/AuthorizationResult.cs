namespace NS.Framework.Authorization.Abstractions.Permissions;

public sealed record AuthorizationResult(
    bool IsGranted,
    string? DenyReason = null,
    string? Source = null);

public static class AuthorizationDenyReasons
{
    public const string PermissionCodeEmpty = "PermissionCodeEmpty";
    public const string NotDefined = "NotDefined";
    public const string NotGranted = "NotGranted";
}

