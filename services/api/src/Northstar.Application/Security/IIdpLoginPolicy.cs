namespace Northstar.Application.Security;

public interface IIdpLoginPolicy
{
    bool IsEnabled { get; }
    bool IsProviderAllowed(string provider);
}
