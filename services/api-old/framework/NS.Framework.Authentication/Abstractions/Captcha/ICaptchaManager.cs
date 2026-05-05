namespace NS.Framework.Authentication.Abstractions.Captcha;

public interface ICaptchaManager
{
    Task<CaptchaGenerationResult> GenerateAsync(
        CaptchaChannel channel,
        string? scene = null,
        string? principalId = null,
        string? clientIp = null,
        string? deviceId = null,
        CancellationToken ct = default);

    Task<bool> ValidateAsync(
        string identifier,
        string code,
        CaptchaChannel channel = CaptchaChannel.Image,
        CancellationToken ct = default);

}