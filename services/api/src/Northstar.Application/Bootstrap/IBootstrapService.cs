using Northstar.Contracts.Knowledge;

namespace Northstar.Application.Bootstrap;

public interface IBootstrapService
{
    Task<BootstrapResponse> GetBootstrapAsync(CancellationToken cancellationToken = default);
}

