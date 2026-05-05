using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NS.Framework.Core.Abstractions.Time;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace NS.Framework.Authentication.Token;

/// <summary>
/// TokenService（纯算法/纯计算）
/// - AccessToken：JWT 签发/验证
/// - RefreshToken：明文生成（opaque）、hash（HMAC-SHA256）
/// </summary>
public sealed class TokenService
{
    private readonly AuthOptions _authOptions;
    private readonly ISystemClock _clock;
    private readonly JwtSecurityTokenHandler _tokenHandler;

    public TokenService(IOptions<AuthOptions> authOptions, ISystemClock clock)
    {
        _authOptions = authOptions.Value;
        _clock = clock;
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    // ============================================================
    // Access Token (JWT)
    // ============================================================

    public Task<string> GenerateAccessTokenAsync(List<Claim> claims)
    {
        if (claims == null) throw new ArgumentNullException(nameof(claims));
        if (string.IsNullOrWhiteSpace(_authOptions.Jwt.SigningKey))
            throw new InvalidOperationException("Jwt.SigningKey is not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.Jwt.SigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = _clock.UtcNow;

        var token = new JwtSecurityToken(
            issuer: _authOptions.Jwt.Issuer,
            audience: _authOptions.Jwt.Audience,
            claims: claims,
            notBefore: now,
            expires: now.Add(_authOptions.Jwt.AccessTokenLifetime),
            signingCredentials: creds
        );

        return Task.FromResult(_tokenHandler.WriteToken(token));
    }

    public ClaimsPrincipal? GetPrincipalFromToken(string token, bool validateLifetime)
    {
        if (string.IsNullOrWhiteSpace(token)) return null;
        if (string.IsNullOrWhiteSpace(_authOptions.Jwt.SigningKey)) return null;

        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_authOptions.Jwt.SigningKey));

            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = validateLifetime,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _authOptions.Jwt.Issuer,
                ValidAudience = _authOptions.Jwt.Audience,
                IssuerSigningKey = key,
                ClockSkew = TimeSpan.Zero
            };

            return _tokenHandler.ValidateToken(token, parameters, out _);
        }
        catch
        {
            return null;
        }
    }

    // ============================================================
    // Refresh Token (Opaque + Hash)
    // ============================================================

    /// <summary>
    /// 生成 refresh token 明文（opaque string）
    /// - 按 TokenConfig.Length 作为“目标字符长度”生成（更贴合你配置语义）
    /// - 至少 256-bit 强度（>= 32 bytes）
    /// </summary>
    public string GenerateRefreshTokenPlain()
    {
        var targetLen = Math.Max(_authOptions.Token.Length, _authOptions.Token.MinimumRefreshTokenLength);

        // base64urlLen ≈ ceil(bytes*4/3) => bytes ≈ floor(len*3/4)
        // 至少 32 bytes（256-bit）
        var byteLen = Math.Max(32, (int)Math.Floor(targetLen * 0.75));

        var bytes = RandomNumberGenerator.GetBytes(byteLen);
        var token = Base64UrlEncode(bytes);

        // 极少数情况下因取整稍短，补一点，再截断到 targetLen（确保“配置语义稳定”）
        if (token.Length < targetLen)
        {
            var extra = RandomNumberGenerator.GetBytes(8);
            token += Base64UrlEncode(extra);
        }

        return token.Length > targetLen ? token[..targetLen] : token;
    }

    /// <summary>
    /// refresh token hash（HMAC-SHA256）
    /// - 必须使用 Token.HashKey（大厂标准：与 Jwt.SigningKey 分离）
    /// - 输出固定 64 位 HEX（便于索引与查询）
    /// </summary>
    public string HashRefreshToken(string refreshTokenPlain)
    {
        if (string.IsNullOrWhiteSpace(refreshTokenPlain))
            throw new ArgumentException("Refresh token is empty.", nameof(refreshTokenPlain));

        var key = _authOptions.Token.HashKey;
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Token.HashKey is required.");

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(refreshTokenPlain));

        return Convert.ToHexString(hash);
    }

    private static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
