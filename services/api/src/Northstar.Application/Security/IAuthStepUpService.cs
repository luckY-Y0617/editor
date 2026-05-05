namespace Northstar.Application.Security;

public interface IAuthStepUpService
{
    Task EnsureSatisfiedAsync(CancellationToken cancellationToken = default);
}
