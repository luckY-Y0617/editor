namespace Northstar.Application.Security;

public interface IScimBearerTokenAccessor
{
    string? GetBearerToken();
}
