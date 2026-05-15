using System.Security.Cryptography;
using System.Text;
using Northstar.Application.Security;

namespace Northstar.Infrastructure.Security;

public sealed class AesGcmShareLinkTokenProtector : IShareLinkTokenProtector
{
    private const string Prefix = "v1";
    private const int NonceBytes = 12;
    private const int TagBytes = 16;

    private readonly AuthOptions _authOptions;
    private readonly MfaOptions _mfaOptions;

    public AesGcmShareLinkTokenProtector(AuthOptions authOptions, MfaOptions mfaOptions)
    {
        _authOptions = authOptions;
        _mfaOptions = mfaOptions;
    }

    public string Protect(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new InvalidOperationException("Share link token cannot be empty.");
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var plaintext = Encoding.UTF8.GetBytes(token);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(GetKey(), TagBytes);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        return string.Join(".", Prefix, Encode(nonce), Encode(ciphertext), Encode(tag));
    }

    public string Unprotect(string protectedToken)
    {
        var parts = protectedToken.Split('.');
        if (parts.Length != 4 || parts[0] != Prefix)
        {
            throw new InvalidOperationException("Share link token ciphertext is invalid.");
        }

        var nonce = Decode(parts[1]);
        var ciphertext = Decode(parts[2]);
        var tag = Decode(parts[3]);
        var plaintext = new byte[ciphertext.Length];

        using var aes = new AesGcm(GetKey(), TagBytes);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }

    private byte[] GetKey()
    {
        var keyMaterial = !string.IsNullOrWhiteSpace(_mfaOptions.SecretProtectionKey)
            ? _mfaOptions.SecretProtectionKey
            : _authOptions.Jwt.SigningKey;
        if (string.IsNullOrWhiteSpace(keyMaterial))
        {
            throw new InvalidOperationException("Auth:Mfa:SecretProtectionKey or Auth:Jwt:SigningKey is required for share link token protection.");
        }

        return SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
    }

    private static string Encode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Decode(string value)
    {
        var normalized = value.Replace('-', '+').Replace('_', '/');
        normalized = normalized.PadRight(normalized.Length + (4 - normalized.Length % 4) % 4, '=');
        return Convert.FromBase64String(normalized);
    }
}
