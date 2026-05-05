using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Northstar.Application.Security;
using Northstar.Domain.Users;

namespace Northstar.Infrastructure.Security;

public sealed class TokenService : ITokenService
{
    private readonly AuthOptions _options;

    public TokenService(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public GeneratedAccessToken CreateAccessToken(User user)
    {
        var signingKey = GetSigningKey();
        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.AddMinutes(Math.Max(1, _options.Jwt.AccessTokenMinutes));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(ClaimTypes.Name, user.DisplayName),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            _options.Jwt.Issuer,
            _options.Jwt.Audience,
            claims,
            now.UtcDateTime,
            expiresAt.UtcDateTime,
            credentials);

        return new GeneratedAccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public GeneratedRefreshToken CreateRefreshToken(
        Guid userId,
        Guid? familyId = null,
        string? ipAddress = null,
        string? userAgent = null)
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Base64UrlEncoder.Encode(bytes);
        return new GeneratedRefreshToken(
            token,
            HashRefreshToken(token),
            familyId ?? Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddDays(Math.Max(1, _options.Jwt.RefreshTokenDays)),
            ipAddress,
            userAgent);
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        if (string.IsNullOrWhiteSpace(_options.Jwt.SigningKey))
        {
            throw new InvalidOperationException("Auth:Jwt:SigningKey is required.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Jwt.SigningKey));
    }
}
