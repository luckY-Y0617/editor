using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace NS.Framework.Notifycation.Sms;

public interface ISmsSender: ITransientDependency
{
    Task SendAsync(string phoneNumber, string code);
}