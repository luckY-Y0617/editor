namespace NS.Module.Identity.Application.Contracts.Captchas.Dtos;

public class CaptchaOutputDto
{
    /// <summary>
    /// 验证码唯一标识（后续校验时要带回）
    /// </summary>
    public string Id { get; set; } = default!;

    /// <summary>
    /// 图片验证码的 Base64 字符串（前端可直接 data:image/png;base64, 渲染）
    /// </summary>
    public string ImageBase64 { get; set; } = default!;

    /// <summary>
    /// 过期时间（秒）
    /// </summary>
    public int ExpireSeconds { get; set; }
}