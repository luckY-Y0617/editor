using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Northstar.Application.Security;
using Northstar.Contracts.Auth;

namespace Northstar.Api.Controllers;

[ApiController]
[Route("auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IAuthSecurityStateService _securityStateService;
    private readonly IAuthMfaService _mfaService;

    public AuthController(
        IAuthService authService,
        IAuthSecurityStateService securityStateService,
        IAuthMfaService mfaService)
    {
        _authService = authService;
        _securityStateService = securityStateService;
        _mfaService = mfaService;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokenResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.RegisterAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokenResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.LoginAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("idp/login")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokenResponse>> IdpLogin(
        IdpLoginRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.IdpLoginAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthTokenResponse>> Refresh(
        RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _authService.RefreshAsync(request, cancellationToken));
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await _authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(MeResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<MeResponse>> Me(CancellationToken cancellationToken)
    {
        return Ok(await _authService.GetMeAsync(cancellationToken));
    }

    [Authorize]
    [HttpGet("security-state")]
    [ProducesResponseType(typeof(AuthSecurityStateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSecurityStateResponse>> GetSecurityState(CancellationToken cancellationToken)
    {
        return Ok(await _securityStateService.GetSecurityStateAsync(cancellationToken));
    }

    [Authorize]
    [HttpPost("mfa/totp/enroll")]
    [ProducesResponseType(typeof(TotpEnrollmentResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<TotpEnrollmentResponse>> EnrollTotp(CancellationToken cancellationToken)
    {
        return Ok(await _mfaService.EnrollTotpAsync(cancellationToken));
    }

    [Authorize]
    [HttpPost("mfa/totp/verify")]
    [ProducesResponseType(typeof(AuthSecurityStateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSecurityStateResponse>> VerifyTotp(
        VerifyTotpRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _mfaService.VerifyTotpAsync(request, cancellationToken));
    }

    [Authorize]
    [HttpPost("mfa/totp/disable")]
    [ProducesResponseType(typeof(AuthSecurityStateResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<AuthSecurityStateResponse>> DisableTotp(CancellationToken cancellationToken)
    {
        return Ok(await _mfaService.DisableTotpAsync(cancellationToken));
    }
}
