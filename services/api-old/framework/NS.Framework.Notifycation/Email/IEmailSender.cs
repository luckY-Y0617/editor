using Volo.Abp.DependencyInjection;

namespace NS.Framework.Notifycation.Email;

public interface IEmailSender: ITransientDependency
{
    public Task SendAsync(string to, string subject, string body);
}