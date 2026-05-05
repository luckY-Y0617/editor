using Northstar.Application.Common;
using Northstar.Contracts.Common;
using Northstar.Contracts.Security;
using Northstar.Domain.Security;

namespace Northstar.Application.Security;

public sealed class ScimTokenService : IScimTokenService
{
    private readonly IWorkspaceAccessService _workspaceAccessService;
    private readonly IScimTokenRepository _repository;
    private readonly IShareLinkTokenService _tokenService;
    private readonly IAuthStepUpService _stepUpService;
    private readonly IUnitOfWork _unitOfWork;

    public ScimTokenService(
        IWorkspaceAccessService workspaceAccessService,
        IScimTokenRepository repository,
        IShareLinkTokenService tokenService,
        IAuthStepUpService stepUpService,
        IUnitOfWork unitOfWork)
    {
        _workspaceAccessService = workspaceAccessService;
        _repository = repository;
        _tokenService = tokenService;
        _stepUpService = stepUpService;
        _unitOfWork = unitOfWork;
    }

    public async Task<CreateScimTokenResponse> CreateAsync(
        Guid workspaceId,
        CreateScimTokenRequest request,
        CancellationToken cancellationToken = default)
    {
        await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        await _stepUpService.EnsureSatisfiedAsync(cancellationToken);
        var actorId = await _workspaceAccessService.GetRequiredUserIdAsync(cancellationToken);

        var expiresAt = NormalizeExpiresAt(request.ExpiresAt);
        if (expiresAt.HasValue && expiresAt <= DateTimeOffset.UtcNow)
        {
            throw new ApplicationErrorException(ErrorCodes.ValidationError, "SCIM token expiry must be in the future.");
        }

        var rawToken = _tokenService.GenerateToken();
        var token = new ScimToken(
            workspaceId,
            request.Name,
            _tokenService.HashToken(rawToken),
            actorId,
            expiresAt);

        await _repository.AddAsync(token, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateScimTokenResponse(ToDto(token), rawToken);
    }

    public async Task<ScimTokensResponse> GetAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        var tokens = await _repository.GetByWorkspaceAsync(workspaceId, cancellationToken);
        return new ScimTokensResponse(tokens.Select(ToDto).ToArray());
    }

    public async Task RevokeAsync(
        Guid workspaceId,
        Guid tokenId,
        CancellationToken cancellationToken = default)
    {
        await _workspaceAccessService.EnsureCanManageWorkspaceAsync(workspaceId, cancellationToken);
        await _stepUpService.EnsureSatisfiedAsync(cancellationToken);
        var token = await _repository.GetForUpdateAsync(workspaceId, tokenId, cancellationToken);
        if (token is null)
        {
            throw new ApplicationErrorException(ErrorCodes.NotFound, "SCIM token was not found.");
        }

        token.Revoke(DateTimeOffset.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static DateTimeOffset? NormalizeExpiresAt(DateTimeOffset? expiresAt)
    {
        return expiresAt?.ToUniversalTime();
    }

    private static ScimTokenDto ToDto(ScimToken token)
    {
        return new ScimTokenDto(
            token.Id.ToString(),
            token.WorkspaceId.ToString(),
            token.Name,
            token.CreatedBy?.ToString(),
            token.CreatedAt,
            token.ExpiresAt,
            token.RevokedAt,
            token.LastUsedAt);
    }
}
