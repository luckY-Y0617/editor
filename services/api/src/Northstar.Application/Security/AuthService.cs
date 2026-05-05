using System.Text.Json;
using Northstar.Application.Common;
using Northstar.Contracts.Auth;
using Northstar.Contracts.Common;
using Northstar.Domain.Users;

namespace Northstar.Application.Security;

public sealed class AuthService : IAuthService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IAuthRepository _authRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly ITokenService _tokenService;
    private readonly IIdpLoginPolicy _idpLoginPolicy;
    private readonly ICurrentUser _currentUser;
    private readonly IAuthRequestContext _requestContext;
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IAuthRepository authRepository,
        IPasswordHashService passwordHashService,
        ITokenService tokenService,
        IIdpLoginPolicy idpLoginPolicy,
        ICurrentUser currentUser,
        IAuthRequestContext requestContext,
        IUnitOfWork unitOfWork)
    {
        _authRepository = authRepository;
        _passwordHashService = passwordHashService;
        _tokenService = tokenService;
        _idpLoginPolicy = idpLoginPolicy;
        _currentUser = currentUser;
        _requestContext = requestContext;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthTokenResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        if (request.Password.Length < 8)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Password must be at least 8 characters.");
        }

        var existingUser = await _authRepository.FindUserByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Email is already registered.");
        }

        var user = new User(request.DisplayName, email);
        var passwordHash = _passwordHashService.HashPassword(user, request.Password);
        await _authRepository.AddUserAsync(user, cancellationToken);
        await _authRepository.AddOrUpdateCredentialAsync(new UserCredential(user.Id, passwordHash), cancellationToken);
        await _authRepository.AddAuthEventAsync(
            new AuthEvent(user.Id, "auth.register", true, _requestContext.IpAddress, _requestContext.UserAgent),
            cancellationToken);

        var issue = await IssueTokensAsync(user, familyId: null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return issue.Response;
    }

    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _authRepository.FindUserByEmailAsync(email, cancellationToken);
        var credential = user is null
            ? null
            : await _authRepository.GetCredentialAsync(user.Id, cancellationToken);

        if (user is null ||
            credential is null ||
            !_passwordHashService.VerifyPassword(user, credential.PasswordHash, request.Password))
        {
            await _authRepository.AddAuthEventAsync(
                new AuthEvent(user?.Id, "auth.login_failed", false, _requestContext.IpAddress, _requestContext.UserAgent),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Invalid email or password.");
        }

        await _authRepository.AddAuthEventAsync(
            new AuthEvent(user.Id, "auth.login", true, _requestContext.IpAddress, _requestContext.UserAgent),
            cancellationToken);

        var issue = await IssueTokensAsync(user, familyId: null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return issue.Response;
    }

    public async Task<AuthTokenResponse> IdpLoginAsync(
        IdpLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_idpLoginPolicy.IsEnabled)
        {
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "IdP login is not enabled.");
        }

        var provider = NormalizeProvider(request.Provider);
        var externalSubjectId = NormalizeExternalSubjectId(request.ExternalSubjectId);
        var displayName = NormalizeDisplayName(request.DisplayName);
        var email = NormalizeOptionalEmail(request.Email);

        if (!_idpLoginPolicy.IsProviderAllowed(provider))
        {
            await AddIdpAuthEventAsync(
                userId: null,
                succeeded: false,
                provider,
                "provider_not_allowed",
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Forbidden, "IdP provider is not allowed.");
        }

        var externalUser = await _authRepository.FindUserByExternalAsync(
            provider,
            externalSubjectId,
            cancellationToken);
        if (externalUser is not null)
        {
            await EnsureEmailIsNotOwnedByAnotherUserAsync(
                externalUser,
                email,
                provider,
                cancellationToken);

            externalUser.ApplyExternalProfile(
                provider,
                externalSubjectId,
                displayName,
                email ?? externalUser.Email);

            await AddIdpAuthEventAsync(
                externalUser.Id,
                succeeded: true,
                provider,
                reason: null,
                cancellationToken);
            var issue = await IssueTokensAsync(externalUser, familyId: null, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return issue.Response;
        }

        if (email is null)
        {
            await AddIdpAuthEventAsync(
                userId: null,
                succeeded: false,
                provider,
                "missing_linked_user",
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "External identity is not linked.");
        }

        var localUser = await _authRepository.FindUserByEmailAsync(email, cancellationToken);
        if (localUser is null)
        {
            await AddIdpAuthEventAsync(
                userId: null,
                succeeded: false,
                provider,
                "missing_linked_user",
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "External identity is not linked.");
        }

        if (HasConflictingExternalIdentity(localUser, provider, externalSubjectId))
        {
            await AddIdpAuthEventAsync(
                localUser.Id,
                succeeded: false,
                provider,
                "external_identity_conflict",
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Conflict, "User is already linked to a different external identity.");
        }

        localUser.ApplyExternalProfile(provider, externalSubjectId, displayName, email);

        await AddIdpAuthEventAsync(
            localUser.Id,
            succeeded: true,
            provider,
            reason: null,
            cancellationToken);
        var loginIssue = await IssueTokensAsync(localUser, familyId: null, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return loginIssue.Response;
    }

    public async Task<AuthTokenResponse> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await _authRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Refresh token is invalid.");

        var now = DateTimeOffset.UtcNow;
        if (!refreshToken.IsUsable(now))
        {
            await RevokeRefreshTokenFamilyAsync(refreshToken.FamilyId, cancellationToken);
            await _authRepository.AddAuthEventAsync(
                new AuthEvent(refreshToken.UserId, "auth.refresh_reuse", false, _requestContext.IpAddress, _requestContext.UserAgent),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Refresh token is invalid.");
        }

        var user = await _authRepository.FindUserByIdAsync(refreshToken.UserId, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Refresh token user is invalid.");

        var issue = await IssueTokensAsync(user, refreshToken.FamilyId, cancellationToken);
        refreshToken.Rotate(issue.RefreshToken.Id);

        await _authRepository.AddAuthEventAsync(
            new AuthEvent(user.Id, "auth.refresh", true, _requestContext.IpAddress, _requestContext.UserAgent),
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return issue.Response;
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = _tokenService.HashRefreshToken(request.RefreshToken);
        var refreshToken = await _authRepository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);
        if (refreshToken is not null)
        {
            refreshToken.Revoke();
            await _authRepository.AddAuthEventAsync(
                new AuthEvent(refreshToken.UserId, "auth.logout", true, _requestContext.IpAddress, _requestContext.UserAgent),
                cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<MeResponse> GetMeAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
        {
            throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Authentication is required.");
        }

        var user = await _authRepository.FindUserByIdAsync(_currentUser.UserId.Value, cancellationToken)
            ?? throw new ApplicationErrorException(ErrorCodes.Unauthorized, "Current user was not found.");

        var workspaces = await _authRepository.GetUserWorkspacesAsync(user.Id, cancellationToken);
        return new MeResponse(
            ToUserDto(user),
            workspaces
                .Select(workspace => new AuthWorkspaceDto(
                    workspace.WorkspaceId.ToString(),
                    workspace.WorkspaceName,
                    workspace.Role))
                .ToArray());
    }

    private async Task<AuthIssueResult> IssueTokensAsync(
        User user,
        Guid? familyId,
        CancellationToken cancellationToken)
    {
        var accessToken = _tokenService.CreateAccessToken(user);
        var refreshToken = _tokenService.CreateRefreshToken(
            user.Id,
            familyId,
            _requestContext.IpAddress,
            _requestContext.UserAgent);

        var refreshTokenEntity = new RefreshToken(
            user.Id,
            refreshToken.TokenHash,
            refreshToken.FamilyId,
            refreshToken.ExpiresAt,
            refreshToken.IpAddress,
            refreshToken.UserAgent);

        await _authRepository.AddRefreshTokenAsync(refreshTokenEntity, cancellationToken);

        return new AuthIssueResult(
            new AuthTokenResponse(
                accessToken.Token,
                accessToken.ExpiresAt,
                refreshToken.Token,
                refreshToken.ExpiresAt,
                ToUserDto(user)),
            refreshTokenEntity);
    }

    private async Task RevokeRefreshTokenFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        var familyTokens = await _authRepository.GetRefreshTokenFamilyAsync(familyId, cancellationToken);
        foreach (var token in familyTokens)
        {
            token.Revoke();
        }
    }

    private async Task EnsureEmailIsNotOwnedByAnotherUserAsync(
        User externalUser,
        string? email,
        string provider,
        CancellationToken cancellationToken)
    {
        if (email is null ||
            string.Equals(externalUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var emailOwner = await _authRepository.FindUserByEmailAsync(email, cancellationToken);
        if (emailOwner is null || emailOwner.Id == externalUser.Id)
        {
            return;
        }

        await AddIdpAuthEventAsync(
            externalUser.Id,
            succeeded: false,
            provider,
            "email_conflict",
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        throw new ApplicationErrorException(ErrorCodes.Conflict, "Email is already linked to another user.");
    }

    private Task AddIdpAuthEventAsync(
        Guid? userId,
        bool succeeded,
        string provider,
        string? reason,
        CancellationToken cancellationToken)
    {
        var metadata = reason is null
            ? JsonSerializer.Serialize(new { provider }, JsonOptions)
            : JsonSerializer.Serialize(new { provider, reason }, JsonOptions);
        var action = succeeded ? "auth.idp_login" : "auth.idp_login_failed";

        return _authRepository.AddAuthEventAsync(
            new AuthEvent(userId, action, succeeded, _requestContext.IpAddress, _requestContext.UserAgent, metadata),
            cancellationToken);
    }

    private static AuthUserDto ToUserDto(User user)
    {
        return new AuthUserDto(user.Id.ToString(), user.Email ?? string.Empty, user.DisplayName);
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "Email is required.");
        }

        return email.Trim().ToLowerInvariant();
    }

    private static string NormalizeProvider(string provider)
    {
        if (string.IsNullOrWhiteSpace(provider))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "provider is required.");
        }

        return provider.Trim().ToLowerInvariant();
    }

    private static string NormalizeExternalSubjectId(string externalSubjectId)
    {
        if (string.IsNullOrWhiteSpace(externalSubjectId))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "externalSubjectId is required.");
        }

        return externalSubjectId.Trim();
    }

    private static string NormalizeDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "displayName is required.");
        }

        return displayName.Trim();
    }

    private static string? NormalizeOptionalEmail(string? email)
    {
        return string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
    }

    private static bool HasConflictingExternalIdentity(
        User user,
        string provider,
        string externalSubjectId)
    {
        return (user.ExternalProvider is not null || user.ExternalSubjectId is not null) &&
            (user.ExternalProvider != provider || user.ExternalSubjectId != externalSubjectId);
    }

    private sealed record AuthIssueResult(AuthTokenResponse Response, RefreshToken RefreshToken);
}
