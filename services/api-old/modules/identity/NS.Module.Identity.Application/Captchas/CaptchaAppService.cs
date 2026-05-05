using System.Threading;
using System.Threading.Tasks;
using NS.Framework.Authentication.Abstractions.Captcha;
using NS.Module.Identity.Application.Contracts.Captchas;
using NS.Module.Identity.Application.Contracts.Captchas.Dtos;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.Application.Services;

namespace NS.Module.Identity.Application.Captchas;

/// <summary>
/// 验证码相关应用服务
/// </summary>
[ApiController]
[Route("/api/app/captcha")]
public class CaptchaAppService : ApplicationService, ICaptchaAppService
{
    private readonly ICaptchaManager _captchaManager;

    public CaptchaAppService(ICaptchaManager captchaManager)
    {
        _captchaManager = captchaManager;
    }

    /// <summary>
    /// 获取图片验证码
    /// </summary>
    [HttpGet("image-captcha")]
    public async Task<CaptchaOutputDto> GetImageCaptchaAsync(CancellationToken cancellationToken = default)
    {
        var result = await _captchaManager.GenerateAsync(
            channel: CaptchaChannel.Image,
            scene: "login",
            principalId: null,   // 登录页一般是匿名，可以先传 null
            clientIp: null,      // 以后需要限流的话可以注入 IHttpContextAccessor 再补
            deviceId: null,
            ct: cancellationToken);

        return new CaptchaOutputDto
        {
            Id = result.Identifier,
            ImageBase64 = result.ImageBase64 ?? string.Empty,
            ExpireSeconds = (int)result.ExpiresIn.TotalSeconds
        };
    }
}