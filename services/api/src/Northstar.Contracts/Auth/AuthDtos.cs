namespace Northstar.Contracts.Auth;

public sealed record RegisterRequest(string Email, string DisplayName, string Password);

public sealed record LoginRequest(string Email, string Password);

public sealed record IdpLoginRequest(
    string Provider,
    string ExternalSubjectId,
    string? Email,
    string DisplayName);

public sealed record RefreshTokenRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthUserDto(string Id, string Email, string DisplayName);

public sealed record AuthWorkspaceDto(string Id, string Name, string Role);

public sealed record AuthTokenResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    AuthUserDto User);

public sealed record MeResponse(AuthUserDto User, IReadOnlyList<AuthWorkspaceDto> Workspaces);

public sealed record AuthSecurityStateResponse(
    string UserId,
    DateTimeOffset? RecentAuthAt,
    int RecentAuthWindowMinutes,
    bool HasRecentAuth,
    bool MfaEnabled,
    bool MfaVerified,
    DateTimeOffset? MfaVerifiedAt,
    bool StepUpRequiredForHighRiskActions,
    IReadOnlyList<string> StepUpMethods);

public sealed record TotpEnrollmentResponse(
    string MethodId,
    string Secret,
    string ProvisioningUri);

public sealed record VerifyTotpRequest(string Code);
