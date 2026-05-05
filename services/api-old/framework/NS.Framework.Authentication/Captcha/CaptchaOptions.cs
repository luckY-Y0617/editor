namespace NS.Framework.Authentication.Captcha;

/// <summary>
/// ClayMo 自己的 Captcha 配置（不要和 NeoCaptcha.CaptchaOptions 混名）
/// </summary>
public class CaptchaSettings
{
    /// <summary>验证码字符数量</summary>
    public int CharacterCount { get; set; } = 4;

    /// <summary>图片宽度</summary>
    public int Width { get; set; } = 120;

    /// <summary>图片高度</summary>
    public int Height { get; set; } = 40;

    /// <summary>过期时间（秒）</summary>
    public int ExpireSeconds { get; set; } = 120;

    /// <summary>缓存 key 前缀</summary>
    public string CacheKeyPrefix { get; set; } = "Captchas:";
}