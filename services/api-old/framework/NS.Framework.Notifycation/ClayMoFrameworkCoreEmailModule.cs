using Microsoft.Extensions.DependencyInjection;
using NS.Framework.Notifycation.Email.SendCloud;
using NS.Framework.Notifycation.Sms.Aliyun;
using Volo.Abp.Modularity;

namespace NS.Framework.Notifycation;

public class ClayMoFrameworkCoreEmailModule: AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHttpClient();
        var configuration = context.Services.GetConfiguration();
        context.Services.Configure<SendCloudOptions>(configuration.GetSection("SendCloudOptions"));
        context.Services.Configure<AliyunOptions>(configuration.GetSection("AliyunOptions"));
    }
}