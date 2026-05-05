using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NeoCaptcha;
using NS.Framework.Authentication.Abstractions.Captcha;

namespace NS.Framework.Authentication.Captcha;

/// <summary>
/// 默认验证码管理器实现：
/// - 使用 NeoCaptcha 生成图片验证码
/// - 使用 IDistributedCache 存储验证码哈希
/// </summary>
public class CaptchaManager : ICaptchaManager
{
    private readonly IDistributedCache _cache;
    private readonly CaptchaSettings _settings;
    private readonly ILogger<CaptchaManager> _logger;

    public CaptchaManager(
        IDistributedCache cache,
        IOptions<CaptchaSettings> settings,
        ILogger<CaptchaManager> logger)
    {
        _cache = cache;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<CaptchaGenerationResult> GenerateAsync(
        CaptchaChannel channel,
        string? scene = null,
        string? principalId = null,
        string? clientIp = null,
        string? deviceId = null,
        CancellationToken ct = default)
    {
        if (channel != CaptchaChannel.Image)
        {
            // 目前只实现图片验证码，其他渠道以后再扩展
            throw new NotSupportedException($"当前仅支持图片验证码，channel={channel}");
        }

        // 1) 使用 NeoCaptcha 生成图片 + 文本
        var neoOptions = new NeoCaptcha.CaptchaOptions
        {
            CharacterCount   = _settings.CharacterCount,
            Width            = _settings.Width,
            Height           = _settings.Height,
            ImageFormat      = CaptchaImageFormat.PNG,
            IsMultiColorText = true
        };

        var captcha = new NeoCaptcha.Captcha(neoOptions);
        var code    = captcha.Text;                // 随机文本
        var bytes   = captcha.ImageAsByteArray;    // 图片字节

        // 2) ClayMo 自己生成一个 Identifier，当作前端传回的 captchaId
        var identifier = Guid.NewGuid().ToString("N");

        // 3) 把验证码文本的哈希存入缓存（防止明文泄露）
        var cacheKey = BuildCacheKey(identifier, channel);
        var hash     = HashCode(code);

        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(_settings.ExpireSeconds)
        };

        await _cache.SetStringAsync(cacheKey, hash, cacheOptions, ct);

        // 4) 图片转 Base64 返回前端
        var base64 = Convert.ToBase64String((byte[])bytes);

        _logger.LogDebug(
            "Generated Captchas: Id={Id}, Channel={Channel}, Scene={Scene}, Principal={Principal}, Ip={Ip}, Device={Device}",
            identifier, channel, scene, principalId, clientIp, deviceId);

        return new CaptchaGenerationResult(
            Identifier: identifier,
            ImageBase64: base64,
            ExpiresIn: TimeSpan.FromSeconds(_settings.ExpireSeconds));
    }

    public async Task<bool> ValidateAsync(
        string identifier,
        string code,
        CaptchaChannel channel = CaptchaChannel.Image,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(code))
            return false;

        var cacheKey= BuildCacheKey(identifier, channel);
        var storedHash = await _cache.GetStringAsync(cacheKey, ct);

        if (storedHash == null)
        {
            return false;
        }

        var inputHash = HashCode(code.Trim());
        var success   = string.Equals(storedHash, inputHash, StringComparison.Ordinal);

        // 一次性验证码：无论成功失败都删掉
        await _cache.RemoveAsync(cacheKey, ct);

        return success;
    }


    #region helpers

    private string BuildCacheKey(string identifier, CaptchaChannel channel)
        => $"{_settings.CacheKeyPrefix}{channel}:{identifier}";

    private static string HashCode(string code)
    {
        using var sha = SHA256.Create();
        var bytes= Encoding.UTF8.GetBytes(code);
        var hashBytes= sha.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }

    #endregion
}
