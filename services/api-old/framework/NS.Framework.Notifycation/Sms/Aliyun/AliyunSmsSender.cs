using System.Text.Json;
using AlibabaCloud.OpenApiClient.Models;
using AlibabaCloud.SDK.Dysmsapi20170525;
using AlibabaCloud.SDK.Dysmsapi20170525.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp;

namespace NS.Framework.Notifycation.Sms.Aliyun;

public class AliyunSmsSender(ILogger<AliyunSmsSender> logger, IOptions<AliyunOptions> options): ISmsSender
{
    private ILogger<AliyunSmsSender> _logger = logger;
    
    private AliyunOptions _aliyunOptions = options.Value;

    private Client CreateClient()
    {
        Config config = new Config()
        {
            AccessKeyId = _aliyunOptions.AccessKeyId,
            AccessKeySecret = _aliyunOptions.AccessKeySecret,
            Endpoint = _aliyunOptions.Endpoint
        };

        return new Client(config);
    }
    
    public async Task SendAsync(string phoneNumber, string code)
    {
        try
        {
            var client = CreateClient();
            SendSmsRequest request = new SendSmsRequest()
            {
                PhoneNumbers = phoneNumber,
                SignName = _aliyunOptions.Sms.SignName,
                TemplateCode = _aliyunOptions.Sms.TemplateCode,
                TemplateParam = JsonSerializer.Serialize(new { code })
            };

            var response = await client.SendSmsAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "阿里云发送短信失败" + ex.Message);
            throw new UserFriendlyException("阿里云发送短信失败" + ex.Message);
        }
    }
}