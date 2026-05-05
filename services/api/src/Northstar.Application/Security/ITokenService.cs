using Northstar.Domain.Users;

namespace Northstar.Application.Security;

public interface ITokenService
{
    GeneratedAccessToken CreateAccessToken(User user);
    GeneratedRefreshToken CreateRefreshToken(Guid userId, Guid? familyId = null, string? ipAddress = null, string? userAgent = null);
    string HashRefreshToken(string refreshToken);
}

public sealed record GeneratedAccessToken(string Token, DateTimeOffset ExpiresAt);

public sealed record GeneratedRefreshToken(
    string Token,
    string TokenHash,
    Guid FamilyId,
    DateTimeOffset ExpiresAt,
    string? IpAddress,
    string? UserAgent);
