using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NS.Framework.Core.Abstractions.Capability;

public interface ICapabilityProvider
{
    string ModuleName { get; }

    Task<Dictionary<string, object?>> GetCapabilitiesAsync(CancellationToken ct = default);
}
