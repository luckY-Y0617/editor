namespace NS.Framework.Authentication.Abstractions.Captcha;

public record CaptchaGenerationResult(
    string Identifier,
    string? ImageBase64,
    TimeSpan ExpiresIn);