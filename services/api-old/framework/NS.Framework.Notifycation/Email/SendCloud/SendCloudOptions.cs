namespace NS.Framework.Notifycation.Email.SendCloud;

public class SendCloudOptions
{
    //SendCloud 邮件服务帐户的用户名或 API 用户标识符，用来识别发送请求的用户。
    public string? ApiUser { get; set; }
    
    //ApiUser 相关的密钥，用于对 API 请求进行身份验证和授权。它确保只有经过授权的用户才能使用该 API。
    public string? ApiKey { get; set; }
    
    //邮件的发件人地址，即通过 SendCloud 发送的邮件将显示的发件人邮箱地址。该地址通常是你注册 SendCloud 服务时验证过的邮箱地址。
    public string? From { get; set; }
}