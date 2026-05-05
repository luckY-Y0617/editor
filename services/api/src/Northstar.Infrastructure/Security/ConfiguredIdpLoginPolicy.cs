using Microsoft.Extensions.Options;
using Northstar.Application.Security;

namespace Northstar.Infrastructure.Security;

public sealed class ConfiguredIdpLoginPolicy : IIdpLoginPolicy
{
    private readonly AuthOptions _options;

    public ConfiguredIdpLoginPolicy(IOptions<AuthOptions> options)
    {
        _options = options.Value;
    }

    public bool IsEnabled => _options.IdpLogin.Enabled;

    public bool IsProviderAllowed(string provider)
    {
        return _options.IdpLogin.AllowedProviders.Any(allowed =>
            string.Equals(allowed?.Trim(), provider, StringComparison.OrdinalIgnoreCase));
    }
}
