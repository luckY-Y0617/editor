using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NS.Framework.Core.Utilities.Json;

namespace NS.Framework.Notifycation.Email.SendCloud;

public class SendCloudEmailSender(
    ILogger<SendCloudEmailSender> logger,
    IHttpClientFactory httpClientFactory,
    IOptions<SendCloudOptions> options): IEmailSender
{
    private readonly ILogger<SendCloudEmailSender> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly SendCloudOptions _options = options.Value;
    
    public async Task SendAsync(string to, string subject, string body)
    {
        logger.LogInformation($"SendCloud Email To {to} , Subject: {subject} , Body: {body}");

        var postBody = new Dictionary<string, string>();
        postBody.Add("apiUser", _options.ApiUser);
        postBody.Add("apiKey", _options.ApiKey);
        postBody.Add("from", _options.From);
        postBody.Add("to", to);
        postBody.Add("subject", subject);
        postBody.Add("html", body);
        
        using FormUrlEncodedContent content = new FormUrlEncodedContent(postBody);
        var httpClient = _httpClientFactory.CreateClient();
        var response = await httpClient.PostAsync("https://api.sendcloud.net/apiv2/mail/send", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"SendCloud Email failed with status code error {response.StatusCode}");
        }
        
        var responseBody = await response.Content.ReadAsStringAsync();
        var responseModel = JsonHelper.ParseJson<SendCloudResponseModel>(responseBody);

        if (!responseModel.Result)
        {
            throw new HttpRequestException($"发送邮件响应返回失败，状态码：{{respModel.StatusCode}},消息：{{respModel.Message}}");
        }
    }
}