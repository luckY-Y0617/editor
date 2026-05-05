using System.Security.Cryptography;
using System.Text;

namespace Northstar.Application.Security;

public sealed class ShareLinkTokenService : IShareLinkTokenService
{
    private const int TokenBytes = 32;

    public string GenerateToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(TokenBytes);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    public string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
