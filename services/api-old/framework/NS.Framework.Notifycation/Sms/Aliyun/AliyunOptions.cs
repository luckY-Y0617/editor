namespace NS.Framework.Notifycation.Sms.Aliyun;

public class AliyunOptions
{
    /// <summary>
    /// 阿里云访问密钥 ID
    /// </summary>
    public string? AccessKeyId { get; set; }

    /// <summary>
    /// 阿里云访问密钥 Secret
    /// </summary>
    public string? AccessKeySecret { get; set; }
    
    /// <summary>
    /// 访问的域名
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// 阿里云短信服务相关配置
    /// </summary>
    public AliyunSms? Sms { get; set; }
}

/// <summary>
/// 阿里云短信服务配置类
/// </summary>
public class AliyunSms
{
    /// <summary>
    /// 短信签名
    /// </summary>
    public string? SignName { get; set; }

    /// <summary>
    /// 短信模板代码
    /// </summary>
    public string? TemplateCode { get; set; }
}