using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Northstar.Application.Security;

public sealed class TotpService : ITotpService
{
    private const int SecretBytes = 20;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string GenerateSecret()
    {
        var bytes = RandomNumberGenerator.GetBytes(SecretBytes);
        return ToBase32(bytes);
    }

    public string BuildProvisioningUri(string issuer, string accountName, string secret)
    {
        var normalizedIssuer = string.IsNullOrWhiteSpace(issuer) ? "Northstar" : issuer.Trim();
        var normalizedAccount = string.IsNullOrWhiteSpace(accountName) ? "northstar-user" : accountName.Trim();
        var label = Uri.EscapeDataString($"{normalizedIssuer}:{normalizedAccount}");
        return $"otpauth://totp/{label}?secret={secret}&issuer={Uri.EscapeDataString(normalizedIssuer)}&algorithm=SHA1&digits=6&period=30";
    }

    public bool VerifyCode(
        string secret,
        string code,
        DateTimeOffset now,
        int stepSeconds,
        int allowedSkewSteps)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode is null)
        {
            return false;
        }

        var secretBytes = FromBase32(secret);
        var currentCounter = now.ToUnixTimeSeconds() / Math.Max(1, stepSeconds);
        var skew = Math.Max(0, allowedSkewSteps);
        for (var offset = -skew; offset <= skew; offset++)
        {
            var expected = ComputeCode(secretBytes, currentCounter + offset);
            if (FixedEquals(expected, normalizedCode))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeCode(string code)
    {
        var normalized = code.Replace(" ", string.Empty, StringComparison.Ordinal).Trim();
        return normalized.Length == 6 && normalized.All(char.IsDigit)
            ? normalized
            : null;
    }

    private static string ComputeCode(byte[] secret, long counter)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (var i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary =
            ((hash[offset] & 0x7f) << 24) |
            ((hash[offset + 1] & 0xff) << 16) |
            ((hash[offset + 2] & 0xff) << 8) |
            (hash[offset + 3] & 0xff);
        return (binary % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static bool FixedEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(left),
            Encoding.ASCII.GetBytes(right));
    }

    private static string ToBase32(byte[] bytes)
    {
        var output = new StringBuilder((bytes.Length + 4) / 5 * 8);
        var bitBuffer = 0;
        var bitCount = 0;
        foreach (var item in bytes)
        {
            bitBuffer = (bitBuffer << 8) | item;
            bitCount += 8;
            while (bitCount >= 5)
            {
                output.Append(Base32Alphabet[(bitBuffer >> (bitCount - 5)) & 0x1f]);
                bitCount -= 5;
            }
        }

        if (bitCount > 0)
        {
            output.Append(Base32Alphabet[(bitBuffer << (5 - bitCount)) & 0x1f]);
        }

        return output.ToString();
    }

    private static byte[] FromBase32(string input)
    {
        var normalized = input.Trim().Replace("=", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        var output = new List<byte>();
        var bitBuffer = 0;
        var bitCount = 0;
        foreach (var item in normalized)
        {
            var value = Base32Alphabet.IndexOf(item, StringComparison.Ordinal);
            if (value < 0)
            {
                throw new FormatException("Invalid base32 secret.");
            }

            bitBuffer = (bitBuffer << 5) | value;
            bitCount += 5;
            if (bitCount >= 8)
            {
                output.Add((byte)((bitBuffer >> (bitCount - 8)) & 0xff));
                bitCount -= 8;
            }
        }

        return output.ToArray();
    }
}
