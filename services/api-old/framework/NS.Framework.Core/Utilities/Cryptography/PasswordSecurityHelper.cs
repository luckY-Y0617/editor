using System;
using System.Security.Cryptography;

namespace NS.Framework.Core.Utilities.Cryptography;

/// <summary>
/// 密码安全工具类，提供密码哈希和验证功能
/// </summary>
public class PasswordSecurityHelper
{
    private const int SaltSize = 16; // 128 位
    private const int HashSize = 32; // 256 位
    private const int Iterations = 100_000;

    /// <summary>
    /// 生成随机盐值
    /// </summary>
    /// <returns>盐值字节数组</returns>
    public static byte[] GenerateSalt()
    {
        byte[] salt = new byte[SaltSize];
        RandomNumberGenerator.Fill(salt);
        return salt;
    }

    // 将 byte[] 转为 hex string（与 HashHelper 中方法相似）
    private static string BytesToHex(byte[] bytes)
    {
        return BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();
    }

    // 将 hex string 转为 byte[]
    private static byte[] HexToBytes(string hex)
    {
        int length = hex.Length;
        byte[] bytes = new byte[length / 2];
        for (int i = 0; i < bytes.Length; i++)
            bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        return bytes;
    }

    /// <summary>
    /// 创建密码哈希和盐值
    /// </summary>
    /// <param name="password">原始密码</param>
    /// <returns>哈希值和盐值的十六进制字符串元组</returns>
    public static (string HashHex, string SaltHex) HashPassword(string password)
    {
        byte[] salt = GenerateSalt();
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        return (BytesToHex(hash), BytesToHex(salt));
    }

    /// <summary>
    /// 验证密码是否匹配
    /// </summary>
    /// <param name="password">待验证的密码</param>
    /// <param name="hashHex">存储的哈希值（十六进制）</param>
    /// <param name="saltHex">存储的盐值（十六进制）</param>
    /// <returns>密码是否匹配</returns>
    public static bool VerifyPassword(string password, string hashHex, string saltHex)
    {
        byte[] salt = HexToBytes(saltHex);
        byte[] expectedHash = HexToBytes(hashHex);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] actualHash = pbkdf2.GetBytes(HashSize);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}

